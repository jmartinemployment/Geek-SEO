using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SourceDiscoveryServiceTests
{
    [Fact]
    public void ParseDiscoveredSources_parses_json_array()
    {
        const string raw = """
            [
              { "url": "https://www.cdc.gov/flu", "title": "CDC", "anchorText": "CDC flu guidance" },
              { "url": "https://www.nih.gov/research", "title": "NIH", "anchorText": "NIH overview" }
            ]
            """;

        var parsed = SourceDiscoveryService.ParseDiscoveredSources(raw);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("https://www.cdc.gov/flu", parsed[0].Url);
        Assert.Equal("CDC flu guidance", parsed[0].AnchorText);
    }

    [Fact]
    public void ParseDiscoveredSources_strips_markdown_fence()
    {
        const string raw = """
            ```json
            [{"url":"https://www.pewresearch.org/topic","title":"Pew Research","anchorText":"Pew Research"}]
            ```
            """;

        var parsed = SourceDiscoveryService.ParseDiscoveredSources(raw);

        Assert.Single(parsed);
        Assert.Equal("https://www.pewresearch.org/topic", parsed[0].Url);
    }
}
