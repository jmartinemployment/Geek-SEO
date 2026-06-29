using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Services.Filtering;

/// <summary>Human-readable filter disposition for a run (inspection, independent of gate messaging).</summary>
public static class FilterInspectionView
{
    public static object Build(IReadOnlyList<SerpItem> organicItems)
    {
        var included = organicItems.Count(c => c.FilterStatus == FilterStatus.Included);
        var excluded = organicItems.Count(c => c.FilterStatus == FilterStatus.Excluded);
        var pending = organicItems.Count(c => c.FilterStatus == FilterStatus.PendingReview);
        var rejected = organicItems.Count(c => c.FilterStatus == FilterStatus.Rejected);

        var mapped = organicItems
            .OrderBy(c => c.RankGroup)
            .Select(MapItem)
            .ToList();

        return new Dictionary<string, object?>
        {
            ["summary"] = new Dictionary<string, int>
            {
                ["total"] = organicItems.Count,
                ["included"] = included,
                ["excluded"] = excluded,
                ["pending_review"] = pending,
                ["rejected"] = rejected
            },
            ["categories"] = new Dictionary<string, object?>
            {
                ["for_crawl"] = Category(
                    "For crawl (included competitors)",
                    "URLs the pipeline will fetch and analyze.",
                    mapped.Where(i => i["filter_status"]?.ToString() == nameof(FilterStatus.Included))),
                ["pending_review"] = Category(
                    "Needs review",
                    "Ambiguous relevance — not auto-included or excluded.",
                    mapped.Where(i => i["filter_status"]?.ToString() == nameof(FilterStatus.PendingReview))),
                ["rejected"] = Category(
                    "Rejected (off-topic)",
                    "Organic results with no pillar keyword word in URL, title, or snippet.",
                    mapped.Where(i => i["filter_status"]?.ToString() == nameof(FilterStatus.Rejected))),
                ["filtered_out"] = Category(
                    "Filtered out (still citeable)",
                    "Excluded from crawl — reference sites (e.g. Wikipedia), owned domains, .gov, etc. Keep in reports as citations.",
                    mapped.Where(i => i["filter_status"]?.ToString() == nameof(FilterStatus.Excluded)))
            },
            ["items"] = mapped
        };
    }

    private static Dictionary<string, object?> Category(
        string label,
        string description,
        IEnumerable<Dictionary<string, object?>> items) =>
        new()
        {
            ["label"] = label,
            ["description"] = description,
            ["count"] = items.Count(),
            ["items"] = items.ToList()
        };

    private static Dictionary<string, object?> MapItem(SerpItem item) =>
        new()
        {
            ["position"] = item.RankGroup,
            ["title"] = item.Title,
            ["snippet"] = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description,
            ["domain"] = item.Domain,
            ["url"] = item.Url,
            ["filter_status"] = item.FilterStatus?.ToString() ?? "NotFiltered",
            ["filter_category"] = FilterCategory(item),
            ["include_reason"] = item.IncludeReason?.ToString(),
            ["exclude_reason"] = item.ExcludeReason,
            ["filtered"] = item.Filtered,
            ["reason"] = DescribeReason(item)
        };

    private static string FilterCategory(SerpItem item) =>
        item.FilterStatus switch
        {
            FilterStatus.Included => "for_crawl",
            FilterStatus.PendingReview => "pending_review",
            FilterStatus.Rejected => "rejected",
            FilterStatus.Excluded => "filtered_out",
            _ => "not_filtered_yet"
        };

    private static string DescribeReason(SerpItem item) =>
        item.FilterStatus switch
        {
            FilterStatus.Included => item.IncludeReason switch
            {
                IncludeReason.KnownPlatform => "Known platform domain.",
                IncludeReason.CommercialIntent => "Commercial intent in title or snippet.",
                IncludeReason.CompetitorSeed => "Competitor seed domain.",
                IncludeReason.MultiPropertyCascade => "Cascade from another included URL on this domain.",
                IncludeReason.ManualOverride => "Manual override.",
                _ => "Included."
            },
            FilterStatus.Excluded => item.ExcludeReason ?? "Excluded.",
            FilterStatus.Rejected => item.ExcludeReason ?? "Rejected — no keyword overlap.",
            FilterStatus.PendingReview => "Ambiguous relevance; requires manual review.",
            _ => "Filter stage has not run yet."
        };
}
