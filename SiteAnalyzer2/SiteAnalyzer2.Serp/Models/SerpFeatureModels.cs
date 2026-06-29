namespace SiteAnalyzer2.Serp.Models;

public record SerpAiOverviewFeature(
    bool Available,
    string? Text,
    string? StatusMessage);

public record SerpPaidFeature(
    string? Title,
    string Url,
    string? Domain,
    string? Description);

public record SerpSitelinkFeature(string Title, string Url);

public record SerpParsedFeatures(
    IReadOnlyList<SerpAiOverviewFeature> AiOverviews,
    IReadOnlyList<SerpPaidFeature> PaidAds);
