using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static class WritingResearchBenchmarkResolver
{
    public static SerpBenchmarksPayload ToBenchmarks(WritingResearchContext research)
    {
        var organic = research.Organic
            .Select(o => new SerpOrganicResult
            {
                Position = o.Position,
                Url = o.Url,
                Title = o.Title,
                Snippet = o.Snippet,
                Domain = o.Domain,
            })
            .ToList();

        var quality = research.DataQuality switch
        {
            "full" => "good",
            "partial" => "low_sample_count",
            _ => "low_sample_count",
        };

        return new SerpBenchmarksPayload
        {
            AvgWordCount = Math.Max(800, research.Benchmarks.MedianWordCountTop5),
            AvgTitleLength = Math.Max(30, research.Benchmarks.MedianTitleLengthTop10),
            BenchmarkQuality = quality,
            TopDomains = organic.Select(o => o.Domain ?? string.Empty).Where(d => d.Length > 0).Distinct().Take(10).ToList(),
            OrganicResults = organic,
        };
    }

    public static SerpFeatures ToSerpFeatures(WritingResearchContext research) => new()
    {
        HasPeopleAlsoAsk = research.PeopleAlsoAsk.Count > 0,
        HasFeaturedSnippet = !string.IsNullOrWhiteSpace(research.Paf.Text),
        HasAiOverview = string.Equals(research.Paf.Type, "ai_overview", StringComparison.OrdinalIgnoreCase),
    };
}
