using System.Text.Json;
using GeekSeoBackend.Providers.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SerpLocalPlaceParserTests
{
    [Fact]
    public void FromSerperRoot_ReadsWebsiteDomainsAndCoordinates()
    {
        const string json = """
            {
              "places": [
                { "title": "Local MSP", "website": "https://www.acme-it.com", "latitude": 26.46, "longitude": -80.07 },
                { "title": "No site" },
                { "title": "Dup", "website": "https://acme-it.com/contact", "latitude": 26.46, "longitude": -80.07 }
              ]
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var places = SerpLocalPlaceParser.PlacesFromSerperRoot(doc.RootElement);

        Assert.Single(places);
        Assert.Equal("acme-it.com", places[0].Domain);
        Assert.Equal(26.46, places[0].Latitude);
        Assert.Equal(-80.07, places[0].Longitude);
    }

    [Fact]
    public void FromSerperRoot_ReadsLinkFieldAsWebsite()
    {
        const string json = """
            {
              "places": [
                { "title": "Local MSP", "link": "https://www.acme-it.com", "latitude": 26.46, "longitude": -80.07 }
              ]
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var places = SerpLocalPlaceParser.PlacesFromSerperRoot(doc.RootElement);

        Assert.Single(places);
        Assert.Equal("acme-it.com", places[0].Domain);
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
