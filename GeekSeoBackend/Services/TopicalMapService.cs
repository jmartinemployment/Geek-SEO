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
        CancellationToken ct = default)
    {
        await EnsureProjectAsync(userId, projectId, ct);

        var existing = await topicalMaps.GetByProjectAsync(projectId, ct);
        if (existing.IsSuccess && existing.Value?.GeneratedAt is not null
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
        var queryGroups = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Query))
            .GroupBy(r => ClusterKey(r.Query))
            .Select(g => new
            {
                Name = g.Key,
                Queries = g.Select(x => x.Query).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList(),
                Impressions = g.Sum(x => x.Impressions),
            })
            .Where(g => g.Impressions > 0)
            .OrderByDescending(g => g.Impressions)
            .Take(40)
            .ToList();

        return queryGroups.Select(group =>
        {
            var match = FindBestDocument(group.Name, group.Queries, documents);
            var coverage = match switch
            {
                null => "gap",
                { SeoScore: > 0 and < 60 } => "partial",
                _ => "covered",
            };

            return new TopicalMapTopic
            {
                Name = TitleCaseCluster(group.Name),
                Queries = group.Queries,
                Coverage = coverage,
                MatchedDocumentId = match?.Id.ToString(),
                MatchedDocumentTitle = match?.Title,
                TotalImpressions = group.Impressions,
            };
        }).ToList();
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
            .Take(3)
            .ToArray();

        return words.Length > 0 ? string.Join(' ', words) : query.ToLowerInvariant();
    }

    private static string TitleCaseCluster(string key) =>
        string.Join(' ', key.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

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

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "your", "that", "this", "what", "when", "where", "how", "best",
    };
}
