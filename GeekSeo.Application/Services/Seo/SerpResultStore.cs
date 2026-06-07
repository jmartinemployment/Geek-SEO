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
}
