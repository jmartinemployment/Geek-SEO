using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class TopicalMapService(
    IGoogleDataService googleData,
    IContentDocumentRepository documents,
    IProjectRepository projects,
    ITopicalMapRepository topicalMaps,
    ISerpProvider serp,
    IKeywordRepository keywordRepository,
    IKeywordProvider keywordProvider,
    ITopicalHierarchyBuilder hierarchyBuilder)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly TimeSpan MapTtl = TimeSpan.FromDays(14);
    private static readonly TimeSpan RefreshCooldown = TimeSpan.FromHours(24);

    public async Task<TopicalMapResult?> GetCachedAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        await EnsureProjectAsync(userId, projectId, ct);
        var stored = await topicalMaps.GetByProjectAsync(projectId, ct);
        if (!stored.IsSuccess || stored.Value is null)
            return null;
        if (stored.Value.ExpiresAt is null || stored.Value.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        return DeserializeMap(stored.Value);
    }

    public async Task<TopicalMapResult> GenerateAsync(
        Guid userId,
        Guid projectId,
        bool force = false,
        CancellationToken ct = default)
    {
        var project = await EnsureProjectAsync(userId, projectId, ct);

        var existing = await topicalMaps.GetByProjectAsync(projectId, ct);
        if (!force
            && existing.IsSuccess && existing.Value?.GeneratedAt is not null
            && DateTimeOffset.UtcNow - existing.Value.GeneratedAt < RefreshCooldown)
        {
            var cached = DeserializeMap(existing.Value);
            if (cached is not null)
                return cached;
        }

        var docsResult = await documents.GetByProjectAsync(projectId, ct);
        var docs = docsResult.IsSuccess && docsResult.Value is not null
            ? docsResult.Value.ToList()
            : [];

        var rankings = await googleData.GetRankingsAsync(userId, projectId, null, null, 1000, ct);
        var gscRows = rankings.Rows
            .Select(r => new GscQueryRow(r.Query, r.Page, r.Impressions, r.Clicks, r.Position))
            .ToList();

        var serpSignatures = await BuildSerpSignaturesAsync(
            gscRows,
            project.DefaultLocation,
            project.DefaultLanguage,
            project.Url,
            ct);

        var clusters = TopicClusteringService.ClusterGscQueries(
            gscRows,
            serpSignatures.Signatures,
            serpSignatures.CompetitorsByQuery);

        var keywordMetrics = await LoadKeywordMetricsAsync(projectId, clusters, ct);
        var topics = clusters
            .Select(c => ToTopic(c, docs, keywordMetrics, project.Url))
            .ToList();

        var now = DateTimeOffset.UtcNow;
        var recommendations = topics
            .Where(t => t.Coverage is "gap" or "partial" or "opportunity")
            .OrderByDescending(t => t.PriorityScore)
            .Take(10)
            .ToList();

        var result = new TopicalMapResult
        {
            Version = 2,
            ProjectId = projectId,
            GeneratedAt = now.ToString("O"),
            ExpiresAt = now.Add(MapTtl).ToString("O"),
            Topics = topics,
            CoveredCount = topics.Count(t => t.Coverage == "covered"),
            GapCount = topics.Count(t => t.Coverage == "gap"),
            PartialCount = topics.Count(t => t.Coverage == "partial"),
            OpportunityCount = topics.Count(t => t.Coverage == "opportunity"),
            Recommendations = recommendations,
        };

        await topicalMaps.UpsertAsync(new SeoTopicalMap
        {
            ProjectId = projectId,
            Status = "ready",
            ClustersJson = JsonSerializer.Serialize(topics, JsonOptions),
            ContentGapsJson = JsonSerializer.Serialize(
                topics.Where(t => t.Coverage is "gap" or "opportunity").ToList(),
                JsonOptions),
            GeneratedAt = now,
            ExpiresAt = now.Add(MapTtl),
        }, ct);

        return result;
    }

    public async Task<TopicalMapResult> GenerateSeedModeAsync(
        Guid userId,
        Guid projectId,
        string seedKeyword,
        string? location = null,
        CancellationToken ct = default)
    {
        var project = await EnsureProjectAsync(userId, projectId, ct);
        location ??= project.DefaultLocation;

        var keywordSuggestionsResult = await keywordProvider.GetKeywordSuggestionsAsync(
            seedKeyword,
            location,
            50,
            ct);

        if (!keywordSuggestionsResult.IsSuccess || keywordSuggestionsResult.Value is null)
            return CreateEmptyResult(projectId, "seed", seedKeyword);

        var suggestions = keywordSuggestionsResult.Value.ToList();
        var topics = new List<TopicalMapTopic>();

        foreach (var suggestion in suggestions)
        {
            var topic = new TopicalMapTopic
            {
                Name = suggestion.Keyword,
                MainKeyword = suggestion.Keyword,
                Queries = [suggestion.Keyword],
                SearchVolume = (int?)suggestion.SearchVolume,
                KeywordDifficulty = (decimal)suggestion.KeywordDifficulty,
                Coverage = "gap",
                TotalImpressions = 0,
                AveragePosition = 0,
                PriorityScore = 0,
                ClusterMethod = "seed",
                CompetitorDomains = [],
            };
            topics.Add(topic);
        }

        var assignedTopics = await hierarchyBuilder.AssignTiersAsync(topics.ToArray(), ct);

        var docsResult = await documents.GetByProjectAsync(projectId, ct);
        var docs = docsResult.IsSuccess && docsResult.Value is not null
            ? docsResult.Value.ToList()
            : [];

        var enrichedTopics = new List<TopicalMapTopic>();
        foreach (var topic in assignedTopics)
        {
            var docMatch = FindBestDocument(topic.MainKeyword ?? topic.Name, [topic.Name], docs);
            var coverage = "gap";
            string? matchSource = null;
            string? matchedDocumentId = null;
            string? matchedDocumentTitle = null;

            if (docMatch is not null)
            {
                matchSource = "document";
                matchedDocumentId = docMatch.Id.ToString();
                matchedDocumentTitle = docMatch.Title;
                coverage = docMatch.SeoScore is > 0 and < 60 ? "partial" : "covered";
            }

            enrichedTopics.Add(topic with
            {
                Coverage = coverage,
                MatchSource = matchSource,
                MatchedDocumentId = matchedDocumentId,
                MatchedDocumentTitle = matchedDocumentTitle,
            });
        }

        var now = DateTimeOffset.UtcNow;
        var recommendations = enrichedTopics
            .Where(t => t.Coverage is "gap" or "partial")
            .OrderByDescending(t => t.SearchVolume ?? 0)
            .Take(10)
            .ToList();

        var pillarCount = enrichedTopics.Count(t => t.Tier == TopicalTier.Pillar);
        var clusterCount = enrichedTopics.Count(t => t.Tier == TopicalTier.Cluster);
        var articleCount = enrichedTopics.Count(t => t.Tier == TopicalTier.Article);

        var result = new TopicalMapResult
        {
            Version = 2,
            ProjectId = projectId,
            GeneratedAt = now.ToString("O"),
            ExpiresAt = now.Add(MapTtl).ToString("O"),
            Topics = enrichedTopics,
            Mode = "seed",
            SeedKeyword = seedKeyword,
            CoveredCount = enrichedTopics.Count(t => t.Coverage == "covered"),
            GapCount = enrichedTopics.Count(t => t.Coverage == "gap"),
            PartialCount = enrichedTopics.Count(t => t.Coverage == "partial"),
            OpportunityCount = enrichedTopics.Count(t => t.Coverage == "opportunity"),
            Recommendations = recommendations,
            PillarCount = pillarCount,
            ClusterCount = clusterCount,
            ArticleCount = articleCount,
        };

        await topicalMaps.UpsertAsync(new SeoTopicalMap
        {
            ProjectId = projectId,
            Status = "ready",
            ClustersJson = JsonSerializer.Serialize(enrichedTopics, JsonOptions),
            ContentGapsJson = JsonSerializer.Serialize(
                enrichedTopics.Where(t => t.Coverage is "gap" or "opportunity").ToList(),
                JsonOptions),
            GeneratedAt = now,
            ExpiresAt = now.Add(MapTtl),
        }, ct);

        return result;
    }

    private static TopicalMapResult CreateEmptyResult(Guid projectId, string mode, string seedKeyword)
    {
        var now = DateTimeOffset.UtcNow;
        return new TopicalMapResult
        {
            Version = 2,
            ProjectId = projectId,
            GeneratedAt = now.ToString("O"),
            ExpiresAt = now.Add(MapTtl).ToString("O"),
            Topics = [],
            Mode = mode,
            SeedKeyword = seedKeyword,
            CoveredCount = 0,
            GapCount = 0,
            PartialCount = 0,
            OpportunityCount = 0,
            Recommendations = [],
            PillarCount = 0,
            ClusterCount = 0,
            ArticleCount = 0,
        };
    }

    private async Task<SeoProject> EnsureProjectAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new InvalidOperationException("Project not found");
        return project.Value;
    }

    private static TopicalMapResult? DeserializeMap(SeoTopicalMap row)
    {
        try
        {
            var topics = JsonSerializer.Deserialize<List<TopicalMapTopic>>(row.ClustersJson, JsonOptions) ?? [];
            var recommendations = topics
                .Where(t => t.Coverage is "gap" or "partial" or "opportunity")
                .OrderByDescending(t => t.PriorityScore)
                .Take(10)
                .ToList();

            return new TopicalMapResult
            {
                Version = topics.Count > 0 && topics[0].PriorityScore > 0 ? 2 : 1,
                ProjectId = row.ProjectId,
                GeneratedAt = row.GeneratedAt?.ToString("O") ?? DateTimeOffset.UtcNow.ToString("O"),
                ExpiresAt = row.ExpiresAt?.ToString("O"),
                Topics = topics,
                CoveredCount = topics.Count(t => t.Coverage == "covered"),
                GapCount = topics.Count(t => t.Coverage == "gap"),
                PartialCount = topics.Count(t => t.Coverage == "partial"),
                OpportunityCount = topics.Count(t => t.Coverage == "opportunity"),
                Recommendations = recommendations,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static IReadOnlyList<TopicalMapTopic> BuildTopics(
        IReadOnlyList<GoogleRankingRow> rows,
        IReadOnlyList<SeoContentDocument> documents)
    {
        var gscRows = rows
            .Select(r => new GscQueryRow(r.Query, r.Page, r.Impressions, r.Clicks, r.Position))
            .ToList();
        var clusters = TopicClusteringService.ClusterGscQueries(gscRows);
        return clusters.Select(c => ToTopic(c, documents, new Dictionary<string, SeoKeyword>(StringComparer.OrdinalIgnoreCase), null)).ToList();
    }

    private async Task<Dictionary<string, SeoKeyword>> LoadKeywordMetricsAsync(
        Guid projectId,
        IReadOnlyList<TopicClusteringService.QueryClusterDraft> clusters,
        CancellationToken ct)
    {
        var cached = await keywordRepository.GetByProjectAsync(projectId, ct);
        if (!cached.IsSuccess || cached.Value is null)
            return new Dictionary<string, SeoKeyword>(StringComparer.OrdinalIgnoreCase);

        return cached.Value.ToDictionary(k => k.Keyword, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<(Dictionary<string, string> Signatures, Dictionary<string, IReadOnlyList<string>> CompetitorsByQuery)>
        BuildSerpSignaturesAsync(
            IReadOnlyList<GscQueryRow> rows,
            string location,
            string languageCode,
            string projectUrl,
            CancellationToken ct)
    {
        var signatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var competitors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var ownHost = TryGetHost(projectUrl);

        var seedQueries = rows
            .GroupBy(r => r.Query.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var list = g.ToList();
                return new
                {
                    Query = g.Key,
                    Impressions = list.Sum(x => x.Impressions),
                    Share = ComputeDominantShare(list),
                };
            })
            .OrderByDescending(x => x.Impressions)
            .Where(x => x.Share < 0.5)
            .Take(TopicClusteringService.MaxSerpSeedQueries)
            .Select(x => x.Query)
            .ToList();

        foreach (var query in seedQueries)
        {
            if (signatures.ContainsKey(query))
                continue;

            var serpResult = await serp.GetSerpResultsAsync(new SerpRequest
            {
                Keyword = query,
                Location = location,
                LanguageCode = languageCode,
                ResultCount = TopicClusteringService.SerpDepth,
            }, ct);

            if (!serpResult.IsSuccess || serpResult.Value is null)
                continue;

            var urls = serpResult.Value.OrganicResults
                .OrderBy(o => o.Position)
                .Select(o => o.Url)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToList();

            var signature = TopicClusteringService.BuildSerpSignature(urls);
            if (!string.IsNullOrWhiteSpace(signature))
                signatures[query] = signature;

            competitors[query] = urls
                .Select(u => TryGetHost(u))
                .Where(h => !string.IsNullOrWhiteSpace(h)
                    && !string.Equals(h, ownHost, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .Cast<string>()
                .ToList();
        }

        return (signatures, competitors);
    }

    private static double ComputeDominantShare(IReadOnlyList<GscQueryRow> rows)
    {
        var total = rows.Sum(r => r.Impressions);
        if (total == 0)
            return 0;

        var topPage = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Page))
            .GroupBy(r => TopicClusteringService.NormalizePageUrl(r.Page), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Sum(x => x.Impressions))
            .OrderByDescending(x => x)
            .FirstOrDefault();

        return topPage / (double)total;
    }

    private static TopicalMapTopic ToTopic(
        TopicClusteringService.QueryClusterDraft cluster,
        IReadOnlyList<SeoContentDocument> documents,
        IReadOnlyDictionary<string, SeoKeyword> keywordMetrics,
        string? projectUrl)
    {
        var docMatch = FindBestDocument(cluster.TopQuery, cluster.Queries, documents);
        var hasPage = !string.IsNullOrWhiteSpace(cluster.DominantPageUrl);
        var isHomepage = hasPage && TopicClusteringService.IsHomepageUrl(cluster.DominantPageUrl!);

        string coverage;
        string? matchSource = null;
        string? matchedPageUrl = null;
        string? matchedDocumentId = null;
        string? matchedDocumentTitle = null;

        if (hasPage && !isHomepage && cluster.DominantPageShare >= 0.35)
        {
            matchedPageUrl = cluster.DominantPageUrl;
            matchSource = "gsc";
            coverage = cluster.AveragePosition <= 12 && cluster.TotalImpressions >= 20
                ? "covered"
                : "partial";
        }
        else if (hasPage && isHomepage)
        {
            matchedPageUrl = cluster.DominantPageUrl;
            matchSource = "gsc";
            coverage = "partial";
        }
        else if (docMatch is not null)
        {
            matchedDocumentId = docMatch.Id.ToString();
            matchedDocumentTitle = docMatch.Title;
            matchSource = "document";
            coverage = docMatch.SeoScore is > 0 and < 60 ? "partial" : "covered";
        }
        else
        {
            coverage = cluster.ClusterMethod == "serp" ? "opportunity" : "gap";
        }

        keywordMetrics.TryGetValue(cluster.TopQuery, out var metrics);
        var mainKeyword = cluster.TopQuery;
        var name = hasPage && !isHomepage
            ? TopicClusteringService.TitleFromPageUrl(cluster.DominantPageUrl!)
            : TitleCaseQuery(cluster.TopQuery);
        var pillar = TopicClusteringService.AssignPillar(cluster.DominantPageUrl, cluster.TopQuery);
        var priority = TopicClusteringService.ComputePriorityScore(
            cluster.TotalImpressions,
            cluster.AveragePosition,
            coverage,
            metrics?.KeywordDifficulty);

        return new TopicalMapTopic
        {
            Name = name,
            Queries = cluster.Queries,
            Coverage = coverage,
            MatchedDocumentId = matchedDocumentId,
            MatchedDocumentTitle = matchedDocumentTitle,
            MatchedPageUrl = matchedPageUrl,
            MatchSource = matchSource,
            TotalImpressions = cluster.TotalImpressions,
            MainKeyword = mainKeyword,
            PillarName = pillar,
            SearchVolume = metrics?.SearchVolume,
            KeywordDifficulty = metrics?.KeywordDifficulty,
            Intent = metrics?.Intent,
            AveragePosition = Math.Round(cluster.AveragePosition, 1),
            PriorityScore = priority,
            ClusterMethod = cluster.ClusterMethod,
            CompetitorDomains = cluster.CompetitorDomains,
        };
    }

    private static SeoContentDocument? FindBestDocument(
        string topQuery,
        IReadOnlyList<string> queries,
        IReadOnlyList<SeoContentDocument> documents)
    {
        SeoContentDocument? best = null;
        var bestScore = 0d;
        var clusterKey = TopicClusteringService.ClusterKeyFromQuery(topQuery);

        foreach (var doc in documents)
        {
            var overlap = Math.Max(
                KeywordOverlap(clusterKey, doc.TargetKeyword),
                queries.Max(q => KeywordOverlap(q, doc.TargetKeyword)));

            if (overlap > bestScore)
            {
                bestScore = overlap;
                best = doc;
            }
        }

        return bestScore >= 0.2 ? best : null;
    }

    private static string TitleCaseQuery(string query) =>
        string.Join(' ', query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

    private static double KeywordOverlap(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return 0;

        var setA = a.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var setB = b.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (setA.Count == 0 || setB.Count == 0)
            return 0;

        return setA.Intersect(setB).Count() / (double)Math.Max(setA.Count, setB.Count);
    }

    private static string? TryGetHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
}
