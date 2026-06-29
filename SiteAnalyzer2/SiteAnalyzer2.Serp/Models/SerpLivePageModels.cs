using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Serp.Models;

public record SerpParsedLink(string Title, string Url);

public record SerpParsedRelatedQuery(int Sequence, string QueryText, SerpRelatedQueryType QueryType);

public record SerpParsedItem(
    string Type,
    int RankGroup,
    int RankAbsolute,
    int Page,
    string Position = "left",
    string? Xpath = null,
    string? Domain = null,
    string? Title = null,
    string? Url = null,
    string? CacheUrl = null,
    string? RelatedSearchUrl = null,
    string? Breadcrumb = null,
    string? WebsiteName = null,
    bool IsImage = false,
    bool IsVideo = false,
    bool IsFeaturedSnippet = false,
    bool IsMalicious = false,
    bool IsWebStory = false,
    string? Description = null,
    string? PreSnippet = null,
    string? ExtendedSnippet = null,
    string? ImagesJson = null,
    bool AmpVersion = false,
    string? RatingJson = null,
    string? PriceJson = null,
    string? FaqJson = null,
    string? ExtendedPeopleAlsoSearchJson = null,
    string? AboutThisResultJson = null,
    string? RelatedResultJson = null,
    DateTime? Timestamp = null,
    bool Ads = false,
    bool? AiOverviewAvailable = null,
    string? AiOverviewMarkdown = null,
    string? AiOverviewStatusMessage = null,
    IReadOnlyList<SerpParsedLink>? Links = null,
    IReadOnlyList<string>? Highlighted = null,
    IReadOnlyList<SerpParsedRelatedQuery>? RelatedQueries = null);

public record SerpLivePageParseResult(
    string Keyword,
    int LocationCode,
    string LanguageCode,
    string Device,
    string Os,
    int Depth,
    string SeDomain,
    string CheckUrl,
    DateTime CapturedAtUtc,
    long? SeResultsCount,
    int PagesCount,
    IReadOnlyList<string> ItemTypes,
    bool LocalPackPresent,
    bool ShoppingResultsPresent,
    IReadOnlyList<SerpParsedItem> Items);
