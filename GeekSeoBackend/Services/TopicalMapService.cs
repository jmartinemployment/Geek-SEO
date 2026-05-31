using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed class TopicalMapService(
    IGoogleDataService googleData,
    IContentDocumentRepository documents,
    IProjectRepository projects)
{
    public async Task<TopicalMapResult> GenerateAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            throw new InvalidOperationException("Project not found");

        var docsResult = await documents.GetByProjectAsync(projectId, ct);
        var docs = docsResult.IsSuccess && docsResult.Value is not null
            ? docsResult.Value.ToList()
            : [];

        var rankings = await googleData.GetRankingsAsync(userId, projectId, null, null, 1000, ct);
        var topics = BuildTopics(rankings.Rows, docs);

        return new TopicalMapResult
        {
            ProjectId = projectId,
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O"),
            Topics = topics,
            CoveredCount = topics.Count(t => t.Coverage == "covered"),
            GapCount = topics.Count(t => t.Coverage == "gap"),
            PartialCount = topics.Count(t => t.Coverage == "partial"),
        };
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
