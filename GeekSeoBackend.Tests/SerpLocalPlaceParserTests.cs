using System.Text.Json;
using GeekSeoBackend.Providers.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SerpLocalPlaceParserTests
{
    [Fact]
    public void FromSerperRoot_ReadsWebsiteDomains()
    {
        const string json = """
            {
              "places": [
                { "title": "Local MSP", "website": "https://www.acme-it.com" },
                { "title": "No site" },
                { "title": "Dup", "website": "https://acme-it.com/contact" }
              ]
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var domains = SerpLocalPlaceParser.FromSerperRoot(doc.RootElement);

        Assert.Single(domains);
        Assert.Equal("acme-it.com", domains[0]);
    }

    [Fact]
    public void FromSerpApiRoot_ReadsLinksWebsite()
    {
        const string json = """
            {
              "local_results": [
                {
                  "title": "Delray IT",
                  "links": { "website": "https://delraytech.com" }
                }
              ]
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var domains = SerpLocalPlaceParser.FromSerpApiRoot(doc.RootElement);

        Assert.Single(domains);
        Assert.Equal("delraytech.com", domains[0]);
    }
}
