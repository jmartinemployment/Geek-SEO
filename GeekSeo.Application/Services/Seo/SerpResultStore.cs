using System.Text.Json;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public static class SerpResultStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static SerpResult? FromDbRow(SeoSerpResult row)
    {
        SerpBenchmarksPayload? benchmarks = null;
        SerpFeatures? features = null;
        IReadOnlyList<PeopleAlsoAskResult> peopleAlsoAsk = [];
        IReadOnlyList<string> relatedSearches = [];

        try
        {
            benchmarks = JsonSerializer.Deserialize<SerpBenchmarksPayload>(row.ResultsJson, JsonOptions);
            features = JsonSerializer.Deserialize<SerpFeatures>(row.SerpFeaturesJson, JsonOptions);
            peopleAlsoAsk = JsonSerializer.Deserialize<IReadOnlyList<PeopleAlsoAskResult>>(row.PeopleAlsoAskJson, JsonOptions) ?? [];
            relatedSearches = JsonSerializer.Deserialize<IReadOnlyList<string>>(row.RelatedSearchesJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return null;
        }

        if (benchmarks?.OrganicResults is not { Count: > 0 })
            return null;

        return new SerpResult
        {
            Keyword = row.Keyword,
            Location = row.Location,
            OrganicResults = benchmarks.OrganicResults,
            PeopleAlsoAsk = peopleAlsoAsk,
            RelatedSearches = relatedSearches,
            FeaturedSnippetText = row.FeaturedSnippet,
            Features = features ?? new SerpFeatures(),
            FetchedAt = row.FetchedAt,
        };
    }

    public static SeoSerpResult ToEphemeralRow(
        SerpResult serp,
        SerpBenchmarksPayload benchmarks,
        string languageCode,
        int retentionDays)
    {
        var fetchedAt = serp.FetchedAt == default ? DateTimeOffset.UtcNow : serp.FetchedAt;
        return new SeoSerpResult
        {
            Id = Guid.NewGuid(),
            Keyword = serp.Keyword,
            Location = serp.Location,
            LanguageCode = languageCode,
            ResultsJson = JsonSerializer.Serialize(benchmarks, JsonOptions),
            PeopleAlsoAskJson = JsonSerializer.Serialize(serp.PeopleAlsoAsk, JsonOptions),
            RelatedSearchesJson = JsonSerializer.Serialize(serp.RelatedSearches, JsonOptions),
            FeaturedSnippet = serp.FeaturedSnippetText,
            SerpFeaturesJson = JsonSerializer.Serialize(serp.Features, JsonOptions),
            FetchedAt = fetchedAt,
            ExpiresAt = fetchedAt.AddDays(Math.Max(1, retentionDays)),
        };
    }
}
