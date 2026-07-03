using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ManualCitationLaneSerpFilterTests
{
    private readonly ManualCitationLaneSerpFilter _filter = new();

    [Fact]
    public void FilterOrganicResults_wiki_lane_keeps_wikipedia_urls_only()
    {
        var result = new SerpResult
        {
            Keyword = "test",
            Location = "United States",
            OrganicResults =
            [
                new SerpOrganicResult
                {
                    Position = 1,
                    Url = "https://en.wikipedia.org/wiki/Customer_journey",
                    Title = "Wiki",
                    Snippet = "x",
                },
                new SerpOrganicResult
                {
                    Position = 2,
                    Url = "https://aisdr.wiki/foo",
                    Title = "Fake",
                    Snippet = "x",
                },
            ],
            Features = new SerpFeatures(),
            FetchedAt = DateTimeOffset.UtcNow,
        };

        var filtered = _filter.FilterOrganicResults(result, SerpResearchLanes.Wiki);

        Assert.Single(filtered);
        Assert.Contains("wikipedia.org", filtered[0].Url, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class CitationLaneHostRulesTests
{
    [Theory]
    [InlineData("en.wikipedia.org", true)]
    [InlineData("wikipedia.org", true)]
    [InlineData("aisdr.wiki", false)]
    public void IsWikipediaHost_distinguishes_wikipedia_from_wiki_tld(string host, bool expected)
    {
        Assert.Equal(expected, CitationLaneHostRules.IsWikipediaHost(host));
    }

    [Theory]
    [InlineData("aisdr.wiki", true)]
    [InlineData("en.wikipedia.org", false)]
    public void IsNonWikipediaWikiTld(string host, bool expected)
    {
        Assert.Equal(expected, CitationLaneHostRules.IsNonWikipediaWikiTld(host));
    }
}
