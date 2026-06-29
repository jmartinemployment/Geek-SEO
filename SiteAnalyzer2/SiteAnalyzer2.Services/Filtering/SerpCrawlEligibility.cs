using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.Filtering;

public static class SerpCrawlEligibility
{
    /// <summary>Organic rows eligible for competitor seed crawl after relevance filter.</summary>
    public static List<SerpItem> SelectEligible(
        IEnumerable<SerpItem> items,
        string keyword,
        bool filterApplied)
    {
        var organic = items
            .Where(i => i.Type == SerpItemTypes.Organic && !i.Ads && i.Url != null)
            .ToList();

        if (organic.Count == 0)
            return [];

        if (!filterApplied)
        {
            return organic
                .Where(i => KeywordPathMatcher.ContainsAnyKeywordToken(keyword, i.Url, i.Title, i.Description))
                .ToList();
        }

        return organic
            .Where(i => i.FilterStatus is FilterStatus.Included or FilterStatus.PendingReview)
            .ToList();
    }

    public static string DescribeShortage(IReadOnlyList<SerpItem> organic, bool filterApplied)
    {
        if (organic.Count == 0)
            return "Run has no organic SERP items. Import SERP HTML first.";

        if (!filterApplied)
            return "No organic SERP items matched the pillar keyword after token check.";

        var included = organic.Count(i => i.FilterStatus == FilterStatus.Included);
        var pending = organic.Count(i => i.FilterStatus == FilterStatus.PendingReview);
        var rejected = organic.Count(i => i.FilterStatus == FilterStatus.Rejected);
        var excluded = organic.Count(i => i.FilterStatus == FilterStatus.Excluded);

        return
            $"No crawl-eligible organic SERP items ({included} included, {pending} pending, {rejected} rejected, {excluded} excluded). " +
            "Competitor crawl uses included and pending on-topic rows; rejected and reference rows are skipped.";
    }
}
