using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Services.CompetitorCrawl;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.Rankings;

public sealed record SerpTargetRankResult(
    int? Position,
    string? Url);

/// <summary>Best organic SERP position for the Project URL domain on a run.</summary>
public static class SerpTargetRankResolver
{
    public static SerpTargetRankResult ResolveFromItems(
        string targetSiteUrl,
        IEnumerable<SerpItem> items)
    {
        var targetDomain = OwnedDomainIndexService.NormalizeLookupDomain(
            ResolveLookupDomain(targetSiteUrl));
        if (string.IsNullOrEmpty(targetDomain))
            return new SerpTargetRankResult(null, null);

        SerpItem? best = null;
        var bestRank = int.MaxValue;

        foreach (var item in items)
        {
            if (!IsOrganicCandidate(item))
                continue;

            var itemDomain = OwnedDomainIndexService.NormalizeLookupDomain(
                OwnedDomainIndexService.ResolveItemDomain(item));
            if (!string.Equals(itemDomain, targetDomain, StringComparison.OrdinalIgnoreCase))
                continue;

            var rank = ResolveRank(item);
            if (rank <= 0 || rank >= bestRank)
                continue;

            bestRank = rank;
            best = item;
        }

        if (best is null)
            return new SerpTargetRankResult(null, null);

        return new SerpTargetRankResult(bestRank, best.Url?.Trim());
    }

    internal static bool IsOrganicCandidate(SerpItem item) =>
        string.Equals(item.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase)
        && !item.Ads
        && !string.IsNullOrWhiteSpace(item.Url);

    internal static int ResolveRank(SerpItem item) =>
        item.RankAbsolute > 0 ? item.RankAbsolute : item.RankGroup;

    private static string ResolveLookupDomain(string targetSiteUrl)
    {
        if (string.IsNullOrWhiteSpace(targetSiteUrl))
            return string.Empty;

        return DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(targetSiteUrl.Trim()));
    }
}
