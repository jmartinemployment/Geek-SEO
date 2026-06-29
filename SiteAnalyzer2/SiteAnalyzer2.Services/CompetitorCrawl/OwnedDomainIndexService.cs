using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

/// <summary>
/// Reverse index from owned SERP imports + all competitor crawl pages for a domain.
/// </summary>
public sealed class OwnedDomainIndexService(AppDbContext db)
{
    private const int MaxPositions = 500;

    public Task<OwnedDomainIndexSnapshot> LoadAsync(string domain, CancellationToken ct = default) =>
        LoadAsync(domain, urlPathPrefix: null, ct);

    public async Task<OwnedDomainIndexSnapshot> LoadAsync(
        string domain,
        string? urlPathPrefix,
        CancellationToken ct = default)
    {
        var target = NormalizeLookupDomain(domain);
        if (string.IsNullOrEmpty(target))
            return OwnedDomainIndexSnapshot.Empty;

        var serpRows = await db.SerpItems.AsNoTracking()
            .Where(i => i.Type == SerpItemTypes.Organic && !i.Ads && i.Url != null)
            .Join(
                db.AnalysisRuns.AsNoTracking(),
                i => i.RunId,
                r => r.Id,
                (i, r) => new PositionCandidate(
                    r.Keyword,
                    ResolveRank(i),
                    i.Url!.Trim(),
                    ResolveItemDomain(i),
                    r.MatchedPillarIntent,
                    r.SerpCapturedAt ?? r.CreatedAt,
                    r.Id,
                    "serp"))
            .ToListAsync(ct);

        var crawlRows = await db.CompetitorPages.AsNoTracking()
            .Where(p => p.Url != null && p.Domain != null)
            .Join(
                db.AnalysisRuns.AsNoTracking(),
                p => p.RunId,
                r => r.Id,
                (p, r) => new PositionCandidate(
                    r.Keyword,
                    p.SeedRankAbsolute > 0 ? p.SeedRankAbsolute : 0,
                    p.Url.Trim(),
                    p.Domain.Trim(),
                    r.MatchedPillarIntent,
                    p.CrawledAt,
                    r.Id,
                    "crawl",
                    p.DepthFromSeed ?? 0))
            .ToListAsync(ct);

        var merged = serpRows
            .Concat(crawlRows)
            .Where(row => string.Equals(NormalizeLookupDomain(row.PageDomain), target, StringComparison.OrdinalIgnoreCase))
            .Where(row => MatchesUrlPath(row.Url, urlPathPrefix))
            .ToList();

        if (merged.Count == 0)
            return OwnedDomainIndexSnapshot.Empty;

        var positions = new List<DomainOrganicPositionRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in merged.GroupBy(row => row.Keyword.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var keyword = group.Key;
            var serpBest = group
                .Where(row => row.Source == "serp")
                .OrderBy(row => row.Position > 0 ? row.Position : int.MaxValue)
                .FirstOrDefault();

            if (serpBest is not null)
            {
                var row = ToRow(serpBest, "SERP organic");
                if (seen.Add($"{row.Keyword}|{row.Url}"))
                    positions.Add(row);
            }

            foreach (var crawl in group.Where(row => row.Source == "crawl").OrderBy(row => row.Url))
            {
                var label = crawl.DepthFromSeed == 0 ? "SERP seed" : "crawled page";
                var row = ToRow(crawl, label);
                if (seen.Add($"{row.Keyword}|{row.Url}"))
                    positions.Add(row);
            }
        }

        positions = positions
            .OrderByDescending(p => PathMatchRank(p.PathMatch))
            .ThenBy(p => p.Position > 0 ? p.Position : int.MaxValue)
            .ThenBy(p => p.Keyword, StringComparer.OrdinalIgnoreCase)
            .Take(MaxPositions)
            .ToList();

        return new OwnedDomainIndexSnapshot
        {
            DistinctKeywordCount = positions.Select(p => p.Keyword).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            ContributingImportCount = merged.Select(r => r.RunId).Distinct().Count(),
            Positions = positions,
        };

        DomainOrganicPositionRow ToRow(PositionCandidate candidate, string sourceLabel)
        {
            var pathMatch = KeywordPathMatcher.Score(candidate.Keyword, candidate.Url);
            return new DomainOrganicPositionRow
            {
                Keyword = candidate.Keyword.Trim(),
                Position = candidate.Position,
                Intent = candidate.Intent?.Trim(),
                Url = candidate.Url,
                SerpFeatures = sourceLabel,
                PathMatch = pathMatch,
                UpdatedAt = new DateTimeOffset(DateTime.SpecifyKind(candidate.CapturedAt, DateTimeKind.Utc)),
            };
        }
    }

    private static int PathMatchRank(string? pathMatch) =>
        pathMatch switch
        {
            "exact" => 3,
            "strong" => 2,
            "weak" => 1,
            _ => 0,
        };

    internal static bool MatchesUrlPath(string url, string? pathPrefix)
    {
        if (string.IsNullOrWhiteSpace(pathPrefix))
            return true;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath.TrimEnd('/');
        var want = pathPrefix.TrimEnd('/');
        if (want.Length == 0)
            return true;

        return path.Equals(want, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(want + "/", StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeLookupDomain(string domain)
    {
        var trimmed = domain.Trim().ToLowerInvariant();
        if (trimmed.StartsWith("www.", StringComparison.Ordinal))
            trimmed = trimmed[4..];

        if (trimmed.Contains("://", StringComparison.Ordinal))
            return DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(trimmed));

        return DomainHelper.GetRegistrableDomain(trimmed);
    }

    internal static string ResolveItemDomain(SerpItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Domain))
            return DomainHelper.GetRegistrableDomain(item.Domain.Trim().ToLowerInvariant());

        return DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(item.Url!));
    }

    private static int ResolveRank(SerpItem item) =>
        item.RankAbsolute > 0 ? item.RankAbsolute : item.RankGroup;

    private sealed record PositionCandidate(
        string Keyword,
        int Position,
        string Url,
        string PageDomain,
        string? Intent,
        DateTime CapturedAt,
        Guid RunId,
        string Source,
        int DepthFromSeed = 0);
}

public sealed record OwnedDomainIndexSnapshot
{
    public static OwnedDomainIndexSnapshot Empty { get; } = new();

    public int DistinctKeywordCount { get; init; }
    public int ContributingImportCount { get; init; }
    public IReadOnlyList<DomainOrganicPositionRow> Positions { get; init; } = [];
}
