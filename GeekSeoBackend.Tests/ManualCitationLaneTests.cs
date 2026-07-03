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

public sealed class CitationLaneHtmlPreflightTests
{
    [Fact]
    public void ValidateWiki_accepts_en_wikipedia_result_url()
    {
        const string html = """
            <a href="https://en.wikipedia.org/wiki/Customer_journey">Customer journey</a>
            """;

        Assert.Null(CitationLaneHtmlPreflight.ValidateWiki(html));
    }

    [Fact]
    public void ValidateWiki_rejects_aisdr_wiki_tld()
    {
        const string html = """<a href="https://aisdr.wiki/foo">bad</a>""";

        var message = CitationLaneHtmlPreflight.ValidateWiki(html);
        Assert.NotNull(message);
        Assert.Contains("Wrong wiki SERP", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateWiki_rejects_serp_without_wikipedia_hosts()
    {
        const string html = """
            <a href="https://www.forbes.com/article">Forbes</a>
            <a href="https://example.com/page">Example</a>
            """;

        var message = CitationLaneHtmlPreflight.ValidateWiki(html);
        Assert.NotNull(message);
        Assert.Contains("No wikipedia.org URLs", message, StringComparison.Ordinal);
    }
}
