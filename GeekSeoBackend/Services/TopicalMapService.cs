using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class TopicalMapService(
    IGoogleDataService googleData,
    IContentDocumentRepository documents,
    IProjectRepository projects,
    ITopicalMapRepository topicalMaps)
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
        await EnsureProjectAsync(userId, projectId, ct);

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
        var topics = BuildTopics(rankings.Rows, docs);

        var now = DateTimeOffset.UtcNow;

        var result = new TopicalMapResult
        {
            ProjectId = projectId,
            GeneratedAt = now.ToString("O"),
            ExpiresAt = now.Add(MapTtl).ToString("O"),
            Topics = topics,
            CoveredCount = topics.Count(t => t.Coverage == "covered"),
            GapCount = topics.Count(t => t.Coverage == "gap"),
            PartialCount = topics.Count(t => t.Coverage == "partial"),
        };

        await topicalMaps.UpsertAsync(new SeoTopicalMap
        {
            ProjectId = projectId,
            Status = "ready",
            ClustersJson = JsonSerializer.Serialize(topics, JsonOptions),
            ContentGapsJson = JsonSerializer.Serialize(topics.Where(t => t.Coverage == "gap").ToList(), JsonOptions),
            GeneratedAt = now,
            ExpiresAt = now.Add(MapTtl),
        }, ct);

        return result;
    }

    private async Task EnsureProjectAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new InvalidOperationException("Project not found");
    }

    private static TopicalMapResult? DeserializeMap(SeoTopicalMap row)
    {
        try
        {
            var topics = JsonSerializer.Deserialize<List<TopicalMapTopic>>(row.ClustersJson, JsonOptions) ?? [];
            return new TopicalMapResult
            {
                ProjectId = row.ProjectId,
                GeneratedAt = row.GeneratedAt?.ToString("O") ?? DateTimeOffset.UtcNow.ToString("O"),
                ExpiresAt = row.ExpiresAt?.ToString("O"),
                Topics = topics,
                CoveredCount = topics.Count(t => t.Coverage == "covered"),
                GapCount = topics.Count(t => t.Coverage == "gap"),
                PartialCount = topics.Count(t => t.Coverage == "partial"),
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
        var validRows = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Query) && r.Impressions > 0)
            .ToList();

        var queryClusters = validRows
            .GroupBy(r => ClusterKey(r.Query))
            .Select(BuildQueryCluster)
            .Where(c => c.TotalImpressions > 0)
            .ToList();

        var topics = MergeDedicatedPageClusters(queryClusters)
            .OrderByDescending(c => c.TotalImpressions)
            .Take(40)
            .Select(cluster => ToTopic(cluster, documents))
            .ToList();

        return topics;
    }

    private static QueryCluster BuildQueryCluster(IGrouping<string, GoogleRankingRow> group)
    {
        var rows = group.ToList();
        var queryImpressions = rows
            .GroupBy(r => r.Query.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new QueryImpression(g.Key, g.Sum(x => x.Impressions)))
            .OrderByDescending(q => q.Impressions)
            .ToList();

        var pageImpressions = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Page))
            .GroupBy(r => NormalizePageUrl(r.Page), StringComparer.OrdinalIgnoreCase)
            .Select(g => new PageImpression(
                g.Key,
                g.Sum(x => x.Impressions),
                WeightedAverage(g, x => x.Position, x => x.Impressions)))
            .OrderByDescending(p => p.Impressions)
            .ToList();

        var dominantPage = pageImpressions.FirstOrDefault();
        var dominantShare = dominantPage is null || group.Sum(x => x.Impressions) == 0
            ? 0
            : dominantPage.Impressions / (double)group.Sum(x => x.Impressions);

        return new QueryCluster(
            group.Key,
            queryImpressions.Select(q => q.Query).Take(12).ToList(),
            queryImpressions[0].Query,
            group.Sum(x => x.Impressions),
            dominantPage?.Url,
            dominantShare,
            dominantPage?.Position ?? rows.Average(x => x.Position));
    }

    private static IReadOnlyList<QueryCluster> MergeDedicatedPageClusters(IReadOnlyList<QueryCluster> clusters)
    {
        var merged = new List<QueryCluster>();

        foreach (var group in clusters.GroupBy(MergeGroupKey, StringComparer.OrdinalIgnoreCase))
        {
            var items = group.ToList();
            if (items.Count == 1)
            {
                merged.Add(items[0]);
                continue;
            }

            var topQuery = items.OrderByDescending(c => c.TotalImpressions).First().TopQuery;
            var totalImpressions = items.Sum(c => c.TotalImpressions);
            var dominantPage = items
                .Select(c => c.DominantPageUrl)
                .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));
            var dominantShare = items.Sum(c => c.TotalImpressions) == 0
                ? 0
                : items.Sum(c => c.DominantPageShare * c.TotalImpressions) / items.Sum(c => c.TotalImpressions);
            var avgPosition = WeightedAverage(items, c => c.AveragePosition, c => c.TotalImpressions);

            merged.Add(new QueryCluster(
                items[0].Key,
                items.SelectMany(c => c.Queries).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList(),
                topQuery,
                totalImpressions,
                dominantPage,
                dominantShare,
                avgPosition));
        }

        return merged;
    }

    private static string MergeGroupKey(QueryCluster cluster)
    {
        if (!string.IsNullOrWhiteSpace(cluster.DominantPageUrl)
            && !IsHomepageUrl(cluster.DominantPageUrl))
        {
            return NormalizePageUrl(cluster.DominantPageUrl);
        }

        return $"intent:{cluster.Key}";
    }

    private static TopicalMapTopic ToTopic(QueryCluster cluster, IReadOnlyList<SeoContentDocument> documents)
    {
        var docMatch = FindBestDocument(cluster.Key, cluster.Queries, documents);
        var hasPage = !string.IsNullOrWhiteSpace(cluster.DominantPageUrl);
        var isHomepage = hasPage && IsHomepageUrl(cluster.DominantPageUrl!);

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
            coverage = "gap";
        }

        var topQuery = cluster.TopQuery;
        var name = hasPage && !isHomepage
            ? TitleFromPageUrl(cluster.DominantPageUrl!)
            : TitleCaseQuery(topQuery);

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
        };
    }

    private static SeoContentDocument? FindBestDocument(
        string clusterKey,
        IReadOnlyList<string> queries,
        IReadOnlyList<SeoContentDocument> documents)
    {
        SeoContentDocument? best = null;
        var bestScore = 0d;

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

    private static string ClusterKey(string query)
    {
        var words = query.ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !StopWords.Contains(w))
            .OrderBy(w => w, StringComparer.Ordinal)
            .Take(3)
            .ToArray();

        return words.Length > 0 ? string.Join(' ', words) : query.ToLowerInvariant();
    }

    internal static string TitleFromPageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return TitleCaseQuery(url);

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return "Homepage";

        var slug = segments[^1].Replace('-', ' ').Replace('_', ' ');
        return TitleCaseCluster(slug);
    }

    private static string TitleCaseQuery(string query) =>
        string.Join(' ', query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

    private static string TitleCaseCluster(string key) =>
        string.Join(' ', key.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));

    private static string NormalizePageUrl(string page)
    {
        if (!Uri.TryCreate(page.Trim(), UriKind.Absolute, out var uri))
            return page.Trim();

        var path = uri.AbsolutePath;
        if (path.Length > 1 && path.EndsWith('/'))
            path = path[..^1];

        return $"{uri.Scheme}://{uri.Host}{path}";
    }

    private static bool IsHomepageUrl(string page)
    {
        if (!Uri.TryCreate(page, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath.Trim('/');
        return path.Length == 0;
    }

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

    private static double WeightedAverage<T>(
        IEnumerable<T> items,
        Func<T, double> valueSelector,
        Func<T, long> weightSelector)
    {
        var totalWeight = 0d;
        var weightedSum = 0d;
        foreach (var item in items)
        {
            var weight = weightSelector(item);
            if (weight <= 0)
                continue;
            totalWeight += weight;
            weightedSum += valueSelector(item) * weight;
        }

        return totalWeight <= 0 ? 0 : weightedSum / totalWeight;
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "your", "that", "this", "what", "when", "where", "how", "best",
        "near", "local", "services", "service", "company", "companies",
    };

    private sealed record QueryImpression(string Query, long Impressions);

    private sealed record PageImpression(string Url, long Impressions, double Position);

    private sealed record QueryCluster(
        string Key,
        IReadOnlyList<string> Queries,
        string TopQuery,
        long TotalImpressions,
        string? DominantPageUrl,
        double DominantPageShare,
        double AveragePosition);
}
