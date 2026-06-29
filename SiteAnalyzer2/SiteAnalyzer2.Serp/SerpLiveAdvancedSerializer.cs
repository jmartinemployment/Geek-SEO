using System.Text.Json;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Serp;

/// <summary>
/// Rebuilds DataForSEO Live Advanced JSON from normalized <see cref="SerpItem"/> tables.
/// </summary>
public static class SerpLiveAdvancedSerializer
{
    public static object Build(AnalysisRun run, IReadOnlyList<SerpItem> items)
    {
        var itemTypes = string.IsNullOrWhiteSpace(run.SerpItemTypesJson)
            ? items.Select(i => i.Type).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList()
            : JsonSerializer.Deserialize<List<string>>(run.SerpItemTypesJson) ?? [];

        var serializedItems = items
            .OrderBy(i => i.RankAbsolute)
            .Select(MapItem)
            .ToList();

        return new Dictionary<string, object?>
        {
            ["status_code"] = 20000,
            ["status_message"] = "Ok.",
            ["result_count"] = 1,
            ["data"] = new Dictionary<string, object?>
            {
                ["api"] = "serp",
                ["function"] = "live",
                ["se"] = "google",
                ["se_type"] = "organic",
                ["keyword"] = run.Keyword,
                ["location_code"] = run.SerpLocationCode ?? 2840,
                ["language_code"] = run.SerpLanguageCode,
                ["device"] = run.SerpDevice,
                ["os"] = run.SerpOs,
                ["depth"] = run.SerpDepth
            },
            ["result"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["keyword"] = run.Keyword,
                    ["type"] = "organic",
                    ["se_domain"] = run.SerpSeDomain,
                    ["location_code"] = run.SerpLocationCode ?? 2840,
                    ["language_code"] = run.SerpLanguageCode,
                    ["check_url"] = run.SerpCheckUrl,
                    ["datetime"] = (run.SerpCapturedAt ?? run.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss") + " +00:00",
                    ["spell"] = DeserializeJson(run.SerpSpellJson),
                    ["refinement_chips"] = DeserializeJson(run.SerpRefinementChipsJson),
                    ["item_types"] = itemTypes,
                    ["se_results_count"] = run.SerpSeResultsCount,
                    ["pages_count"] = run.SerpPagesCount,
                    ["items_count"] = run.SerpItemsCount > 0 ? run.SerpItemsCount : serializedItems.Count,
                    ["items"] = serializedItems
                }
            }
        };
    }

    private static Dictionary<string, object?> MapItem(SerpItem item)
    {
        if (item.Type == SerpItemTypes.RelatedSearches)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = item.Type,
                ["rank_group"] = item.RankGroup,
                ["rank_absolute"] = item.RankAbsolute,
                ["page"] = item.Page,
                ["position"] = item.Position,
                ["xpath"] = item.Xpath,
                ["items"] = item.RelatedQueries.OrderBy(q => q.Sequence).Select(q => q.QueryText).ToList(),
                ["rectangle"] = DeserializeJson(item.RectangleJson)
            };
        }

        if (item.Type == SerpItemTypes.AiOverview)
        {
            return new Dictionary<string, object?>
            {
                ["type"] = item.Type,
                ["rank_group"] = item.RankGroup,
                ["rank_absolute"] = item.RankAbsolute,
                ["page"] = item.Page,
                ["position"] = item.Position,
                ["available"] = item.AiOverviewAvailable ?? false,
                ["markdown"] = item.AiOverviewAvailable == true ? item.AiOverviewMarkdown : null,
                ["status_message"] = item.AiOverviewStatusMessage
            };
        }

        var mapped = new Dictionary<string, object?>
        {
            ["type"] = item.Ads ? SerpItemTypes.Paid : item.Type,
            ["rank_group"] = item.RankGroup,
            ["rank_absolute"] = item.RankAbsolute,
            ["page"] = item.Page,
            ["position"] = item.Position,
            ["xpath"] = item.Xpath,
            ["domain"] = item.Domain,
            ["title"] = item.Title,
            ["url"] = item.Url,
            ["cache_url"] = item.CacheUrl,
            ["related_search_url"] = item.RelatedSearchUrl,
            ["breadcrumb"] = item.Breadcrumb,
            ["website_name"] = item.WebsiteName,
            ["is_image"] = item.IsImage,
            ["is_video"] = item.IsVideo,
            ["is_featured_snippet"] = item.IsFeaturedSnippet,
            ["is_malicious"] = item.IsMalicious,
            ["is_web_story"] = item.IsWebStory,
            ["description"] = item.Description,
            ["pre_snippet"] = item.PreSnippet,
            ["extended_snippet"] = item.ExtendedSnippet,
            ["images"] = DeserializeJson(item.ImagesJson),
            ["amp_version"] = item.AmpVersion,
            ["rating"] = DeserializeJson(item.RatingJson),
            ["price"] = DeserializeJson(item.PriceJson),
            ["highlighted"] = item.HighlightedPhrases.Count > 0
                ? item.HighlightedPhrases.OrderBy(h => h.Sequence).Select(h => h.Text).ToList()
                : null,
            ["links"] = item.Links.Count > 0
                ? item.Links.OrderBy(l => l.Sequence).Select(l => new Dictionary<string, object?>
                {
                    ["title"] = l.Title,
                    ["url"] = l.Url
                }).ToList()
                : null,
            ["faq"] = DeserializeJson(item.FaqJson),
            ["extended_people_also_search"] = DeserializeJson(item.ExtendedPeopleAlsoSearchJson),
            ["about_this_result"] = DeserializeJson(item.AboutThisResultJson),
            ["related_result"] = DeserializeJson(item.RelatedResultJson),
            ["timestamp"] = item.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") + " +00:00",
            ["rectangle"] = DeserializeJson(item.RectangleJson),
            ["ads"] = item.Ads,
            ["filtered"] = item.Filtered,
            ["filter_status"] = item.FilterStatus?.ToString(),
            ["include_reason"] = item.IncludeReason?.ToString(),
            ["exclude_reason"] = item.ExcludeReason
        };

        return mapped;
    }

    private static object? DeserializeJson(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<JsonElement>(json);
}
