using SiteAnalyzer2.Domain.Entities;

namespace SiteAnalyzer2.Serp;

/// <summary>
/// Exact relational shape stored after HTML import — not the reconstructed DataForSEO JSON.
/// </summary>
public static class SerpPersistedView
{
    public static object Build(AnalysisRun run, IReadOnlyList<SerpItem> items)
    {
        var ordered = items.OrderBy(i => i.RankAbsolute).ToList();
        var links = ordered.SelectMany(i => i.Links).Count();
        var highlighted = ordered.SelectMany(i => i.HighlightedPhrases).Count();
        var relatedQueries = ordered.SelectMany(i => i.RelatedQueries).Count();

        return new Dictionary<string, object?>
        {
            ["schema"] = Schema(),
            ["run_id"] = run.Id,
            ["before_import"] =
                "Only analysis_runs exists until POST /serp/import-html succeeds; then rows below are written.",
            ["run_header"] = RunHeader(run),
            ["table_counts"] = new Dictionary<string, int>
            {
                ["serp_items"] = ordered.Count,
                ["serp_item_links"] = links,
                ["serp_item_highlighted"] = highlighted,
                ["serp_related_queries"] = relatedQueries
            },
            ["serp_items"] = ordered.Select(MapItem).ToList()
        };
    }

    private static IReadOnlyList<Dictionary<string, string>> Schema() =>
    [
        new()
        {
            ["table"] = "analysis_runs",
            ["role"] = "Run + SERP capture header (keyword, check_url, se_results_count, item_types_json, …)"
        },
        new()
        {
            ["table"] = "serp_items",
            ["role"] =
                "One row per SERP element: organic, paid, related_searches, ai_overview. Filter columns filled after Filter stage."
        },
        new()
        {
            ["table"] = "serp_item_links",
            ["role"] = "Sitelinks child rows (FK serp_items.id)"
        },
        new()
        {
            ["table"] = "serp_item_highlighted",
            ["role"] = "Bold/highlighted snippet terms (FK serp_items.id)"
        },
        new()
        {
            ["table"] = "serp_related_queries",
            ["role"] = "Related / PAA / PASF query strings on related_searches items (FK serp_items.id)"
        }
    ];

    private static Dictionary<string, object?> RunHeader(AnalysisRun run) =>
        new()
        {
            ["keyword"] = run.Keyword,
            ["target_site_url"] = run.TargetSiteUrl,
            ["serp_provider_key"] = run.SerpProviderKey,
            ["serp_check_url"] = run.SerpCheckUrl,
            ["serp_captured_at"] = run.SerpCapturedAt,
            ["serp_se_results_count"] = run.SerpSeResultsCount,
            ["serp_pages_count"] = run.SerpPagesCount,
            ["serp_items_count"] = run.SerpItemsCount,
            ["serp_item_types_json"] = run.SerpItemTypesJson,
            ["serp_local_pack_present"] = run.SerpLocalPackPresent,
            ["serp_shopping_results_present"] = run.SerpShoppingResultsPresent,
            ["serp_location_code"] = run.SerpLocationCode,
            ["serp_language_code"] = run.SerpLanguageCode,
            ["serp_device"] = run.SerpDevice,
            ["serp_os"] = run.SerpOs,
            ["serp_depth"] = run.SerpDepth,
            ["serp_se_domain"] = run.SerpSeDomain
        };

    private static Dictionary<string, object?> MapItem(SerpItem item) =>
        new()
        {
            ["table"] = "serp_items",
            ["id"] = item.Id,
            ["project_id"] = item.ProjectId,
            ["run_id"] = item.RunId,
            ["type"] = item.Type,
            ["rank_group"] = item.RankGroup,
            ["rank_absolute"] = item.RankAbsolute,
            ["page"] = item.Page,
            ["position"] = item.Position,
            ["domain"] = item.Domain,
            ["title"] = item.Title,
            ["url"] = item.Url,
            ["breadcrumb"] = item.Breadcrumb,
            ["website_name"] = item.WebsiteName,
            ["description"] = item.Description,
            ["pre_snippet"] = item.PreSnippet,
            ["extended_snippet"] = item.ExtendedSnippet,
            ["ads"] = item.Ads,
            ["filtered"] = item.Filtered,
            ["filter_status"] = item.FilterStatus?.ToString(),
            ["include_reason"] = item.IncludeReason?.ToString(),
            ["exclude_reason"] = item.ExcludeReason,
            ["is_featured_snippet"] = item.IsFeaturedSnippet,
            ["is_video"] = item.IsVideo,
            ["is_image"] = item.IsImage,
            ["ai_overview_available"] = item.AiOverviewAvailable,
            ["ai_overview_markdown"] = item.AiOverviewMarkdown,
            ["ai_overview_status_message"] = item.AiOverviewStatusMessage,
            ["rating_json"] = item.RatingJson,
            ["price_json"] = item.PriceJson,
            ["images_json"] = item.ImagesJson,
            ["faq_json"] = item.FaqJson,
            ["serp_item_links"] = item.Links
                .OrderBy(l => l.Sequence)
                .Select(l => new Dictionary<string, object?>
                {
                    ["table"] = "serp_item_links",
                    ["id"] = l.Id,
                    ["serp_item_id"] = l.SerpItemId,
                    ["sequence"] = l.Sequence,
                    ["title"] = l.Title,
                    ["url"] = l.Url
                })
                .ToList(),
            ["serp_item_highlighted"] = item.HighlightedPhrases
                .OrderBy(h => h.Sequence)
                .Select(h => new Dictionary<string, object?>
                {
                    ["table"] = "serp_item_highlighted",
                    ["id"] = h.Id,
                    ["serp_item_id"] = h.SerpItemId,
                    ["sequence"] = h.Sequence,
                    ["text"] = h.Text
                })
                .ToList(),
            ["serp_related_queries"] = item.RelatedQueries
                .OrderBy(q => q.Sequence)
                .Select(q => new Dictionary<string, object?>
                {
                    ["table"] = "serp_related_queries",
                    ["id"] = q.Id,
                    ["serp_item_id"] = q.SerpItemId,
                    ["sequence"] = q.Sequence,
                    ["query_text"] = q.QueryText,
                    ["query_type"] = q.QueryType.ToString()
                })
                .ToList()
        };
}
