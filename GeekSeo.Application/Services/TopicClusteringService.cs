using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services;

/// <summary>SERP- and GSC-grounded query clustering shared by topical map and keyword planner.</summary>
public static class TopicClusteringService
{
    public const int MaxTopics = 40;
    public const int MaxSerpSeedQueries = 15;
    public const int SerpDepth = 10;

    public sealed record QueryAggregate(
        string Query,
        long TotalImpressions,
        string? DominantPageUrl,
        double DominantPageShare,
        double AveragePosition);

    public sealed record QueryClusterDraft(
        string ClusterKey,
        string ClusterMethod,
        IReadOnlyList<string> Queries,
        string TopQuery,
        long TotalImpressions,
        string? DominantPageUrl,
        double DominantPageShare,
        double AveragePosition,
        IReadOnlyList<string> CompetitorDomains);

    public static string BuildSerpSignature(IReadOnlyList<string> rankingUrls, int take = 3)
    {
        if (rankingUrls.Count == 0)
            return string.Empty;

        return string.Join(
            '|',
            rankingUrls
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(NormalizePageUrl)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
                .Take(take));
    }

    public static IReadOnlyList<QueryClusterDraft> ClusterGscQueries(
        IReadOnlyList<GscQueryRow> rows,
        IReadOnlyDictionary<string, string>? serpSignatureByQuery = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? competitorDomainsByQuery = null)
    {
        var valid = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Query) && r.Impressions > 0)
            .ToList();
        if (valid.Count == 0)
            return [];

        var aggregates = valid
            .GroupBy(r => r.Query.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => AggregateQuery(g.Key, g.ToList()))
            .ToList();

        var keyed = aggregates
            .Select(q => (
                Aggregate: q,
                Key: ResolveClusterKey(q, serpSignatureByQuery)))
            .ToList();

        var clusters = new List<QueryClusterDraft>();
        foreach (var group in keyed.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var members = group.Select(x => x.Aggregate).ToList();
            var method = ResolveClusterMethod(members, serpSignatureByQuery);
            var queries = members
                .OrderByDescending(m => m.TotalImpressions)
                .Select(m => m.Query)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();
            var top = members.OrderByDescending(m => m.TotalImpressions).First();
            if (string.IsNullOrWhiteSpace(top.Query))
                continue;
            var totalImpressions = members.Sum(m => m.TotalImpressions);
            var dominantPage = members
                .Select(m => m.DominantPageUrl)
                .FirstOrDefault(u => !string.IsNullOrWhiteSpace(u) && !IsHomepageUrl(u));
            var dominantShare = totalImpressions == 0
                ? 0
                : members.Sum(m => m.DominantPageShare * m.TotalImpressions) / totalImpressions;
            var avgPosition = WeightedAverage(
                members,
                m => m.AveragePosition,
                m => m.TotalImpressions);

            var competitors = top.Query is not null
                && competitorDomainsByQuery?.TryGetValue(top.Query, out var domains) == true
                ? domains
                : [];

            clusters.Add(new QueryClusterDraft(
                group.Key,
                method,
                queries,
                top.Query,
                totalImpressions,
                dominantPage ?? top.DominantPageUrl,
                dominantShare,
                avgPosition,
                competitors));
        }

        return clusters
            .OrderByDescending(c => c.TotalImpressions)
            .Take(MaxTopics)
            .ToList();
    }

    public static double ComputePriorityScore(
        long impressions,
        double averagePosition,
        string coverage,
        decimal? keywordDifficulty)
    {
        var impFactor = Math.Log10(Math.Max(impressions, 1) + 1);
        var positionFactor = 1.0 / (1.0 + averagePosition / 10.0);
        var coverageFactor = coverage switch
        {
            "gap" or "opportunity" => 1.25,
            "partial" => 1.1,
            _ => 0.85,
        };
        var kdFactor = keywordDifficulty.HasValue
            ? 1.0 - Math.Min((double)keywordDifficulty.Value, 100) / 100.0
            : 0.7;

        return Math.Round(impFactor * positionFactor * coverageFactor * kdFactor * 100, 1);
    }

    public static string AssignPillar(string? pageUrl, string topQuery)
    {
        if (!string.IsNullOrWhiteSpace(pageUrl)
            && Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0)
                return TitleCaseSlug(segments[0]);
        }

        var words = topQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? TitleCaseSlug(words[0]) : "General";
    }

    public static IReadOnlyList<(string ClusterName, string PillarKeyword, IReadOnlyList<string> Keywords)> ClusterKeywordList(
        IReadOnlyList<string> keywords,
        IReadOnlyDictionary<string, string>? serpSignatureByKeyword = null)
    {
        var distinct = keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count == 0)
            return [];

        var groups = distinct
            .GroupBy(k => ResolveKeywordClusterKey(k, serpSignatureByKeyword), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var list = g.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
                var pillar = list[0];
                var label = ClusterLabelFromKey(g.Key, pillar);
                return (label, pillar, (IReadOnlyList<string>)list);
            })
            .OrderByDescending(g => g.Item3.Count)
            .ToList();

        return groups;
    }

    public static string ClusterKeyFromQuery(string query)
    {
        var words = query.ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !StopWords.Contains(w))
            .OrderBy(w => w, StringComparer.Ordinal)
            .Take(3)
            .ToArray();

        return words.Length > 0 ? string.Join(' ', words) : query.ToLowerInvariant();
    }

    public static string NormalizePageUrl(string page)
    {
        if (!Uri.TryCreate(page.Trim(), UriKind.Absolute, out var uri))
            return page.Trim();

        var path = uri.AbsolutePath;
        if (path.Length > 1 && path.EndsWith('/'))
            path = path[..^1];

        return $"{uri.Scheme}://{uri.Host}{path}";
    }

    public static bool IsHomepageUrl(string page)
    {
        if (!Uri.TryCreate(page, UriKind.Absolute, out var uri))
            return false;

        return uri.AbsolutePath.Trim('/').Length == 0;
    }

    private static QueryAggregate AggregateQuery(string query, IReadOnlyList<GscQueryRow> rows)
    {
        var pageGroups = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Page))
            .GroupBy(r => NormalizePageUrl(r.Page), StringComparer.OrdinalIgnoreCase)
            .Select(g => new PageStat(
                g.Key,
                g.Sum(x => x.Impressions),
                WeightedAverage(g, x => x.Position, x => x.Impressions)))
            .OrderByDescending(p => p.Impressions)
            .ToList();

        var totalImpressions = rows.Sum(x => x.Impressions);
        var dominant = pageGroups.FirstOrDefault();
        var share = dominant is null || totalImpressions == 0
            ? 0
            : dominant.Impressions / (double)totalImpressions;

        return new QueryAggregate(
            query,
            totalImpressions,
            dominant?.Url,
            share,
            dominant?.Position ?? rows.Average(x => x.Position));
    }

    private static string ResolveClusterKey(
        QueryAggregate aggregate,
        IReadOnlyDictionary<string, string>? serpSignatureByQuery)
    {
        if (!string.IsNullOrWhiteSpace(aggregate.DominantPageUrl)
            && !IsHomepageUrl(aggregate.DominantPageUrl)
            && aggregate.DominantPageShare >= 0.35)
        {
            return $"page:{NormalizePageUrl(aggregate.DominantPageUrl)}";
        }

        if (serpSignatureByQuery?.TryGetValue(aggregate.Query, out var signature) == true
            && !string.IsNullOrWhiteSpace(signature))
        {
            return $"serp:{signature}";
        }

        return $"token:{ClusterKeyFromQuery(aggregate.Query)}";
    }

    private static string ResolveClusterMethod(
        IReadOnlyList<QueryAggregate> members,
        IReadOnlyDictionary<string, string>? serpSignatureByQuery)
    {
        if (members.Any(m =>
                !string.IsNullOrWhiteSpace(m.DominantPageUrl)
                && !IsHomepageUrl(m.DominantPageUrl)
                && m.DominantPageShare >= 0.35))
        {
            return "gsc_page";
        }

        if (serpSignatureByQuery is not null
            && members.Any(m => serpSignatureByQuery.ContainsKey(m.Query)))
        {
            return "serp";
        }

        return "token";
    }

    private static string ResolveKeywordClusterKey(
        string keyword,
        IReadOnlyDictionary<string, string>? serpSignatureByKeyword)
    {
        if (serpSignatureByKeyword?.TryGetValue(keyword, out var signature) == true
            && !string.IsNullOrWhiteSpace(signature))
        {
            return $"serp:{signature}";
        }

        return $"token:{ClusterKeyFromQuery(keyword)}";
    }

    private static string ClusterLabelFromKey(string key, string fallbackKeyword)
    {
        if (key.StartsWith("page:", StringComparison.Ordinal))
            return TitleFromPageUrl(key["page:".Length..]);

        if (key.StartsWith("serp:", StringComparison.Ordinal))
            return TitleCaseSlug(fallbackKeyword);

        if (key.StartsWith("token:", StringComparison.Ordinal))
            return TitleCaseSlug(key["token:".Length..]);

        return TitleCaseSlug(fallbackKeyword);
    }

    public static string TitleFromPageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return TitleCaseSlug(url);

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return "Homepage";

        return TitleCaseSlug(segments[^1].Replace('-', ' ').Replace('_', ' '));
    }

    private static string TitleCaseSlug(string value) =>
        string.Join(
            ' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

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

    private sealed record PageStat(string Url, long Impressions, double Position);
}
