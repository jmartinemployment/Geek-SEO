using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Services.CompetitorCrawl;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.Filtering;

public static class SerpFilterCounts
{
    public sealed record Summary(
        bool FilterApplied,
        int Included,
        int Excluded,
        int PendingReview,
        int Rejected,
        int CrawlEligible,
        int CompetitorCrawlSeedCount);

    public static Summary FromRunItems(IReadOnlyList<SerpItem> items, string targetSiteUrl, string? pillarKeyword = null)
    {
        var organic = items
            .Where(i => i.Type == SerpItemTypes.Organic && !i.Ads)
            .ToList();

        var filterApplied = organic.Any(i => i.FilterStatus.HasValue);
        var included = organic.Count(i => i.FilterStatus == FilterStatus.Included);
        var excluded = organic.Count(i => i.FilterStatus == FilterStatus.Excluded);
        var pending = organic.Count(i => i.FilterStatus == FilterStatus.PendingReview);
        var rejected = organic.Count(i => i.FilterStatus == FilterStatus.Rejected);
        var crawlEligible = SerpCrawlEligibility.SelectEligible(organic, pillarKeyword ?? "", filterApplied).Count;

        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(targetSiteUrl));
        var crawlItems = SerpCrawlEligibility.SelectEligible(organic, pillarKeyword ?? "", filterApplied);
        var seedCount = CompetitorCrawlService.SelectSeedsPerDomain(crawlItems, targetDomain, pillarKeyword).Count;

        return new Summary(filterApplied, included, excluded, pending, rejected, crawlEligible, seedCount);
    }
}
