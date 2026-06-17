using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.NicheExtraction;

namespace GeekSeoBackend.Tests;

public sealed class LocalCompetitorRadiusTests
{
    [Fact]
    public void CollectLocalCompetitorDomains_WithRadius_OnlyIncludesPlacesWithinRadius()
    {
        var center = new LocalServiceAreaContext(26.4615, -80.0728, 20);
        var serp = new SerpResult
        {
            Keyword = "ai consulting",
            Location = "Delray Beach, Florida, United States",
            OrganicResults =
            [
                new SerpOrganicResult
                {
                    Position = 1,
                    Url = "https://national-ai.example/",
                    Title = "National",
                    Snippet = "",
                    Domain = "national-ai.example",
                },
            ],
            LocalPlaces =
            [
                new SerpLocalPlace("boca-ai.example", 26.3683, -80.1289),
                new SerpLocalPlace("miami-ai.example", 25.7617, -80.1918),
                new SerpLocalPlace("no-coords.example", null, null),
            ],
            Features = new SerpFeatures { HasLocalPack = true },
            FetchedAt = DateTimeOffset.UtcNow,
        };

        var domains = PillarDemandEnricher.CollectLocalCompetitorDomains(serp, center);

        Assert.Single(domains);
        Assert.Equal("boca-ai.example", domains[0]);
    }

    [Fact]
    public void CollectLocalCompetitorDomains_WithoutRadius_IncludesOrganicAndPlaces()
    {
        var serp = new SerpResult
        {
            Keyword = "ai consulting",
            Location = "Delray Beach, Florida, United States",
            OrganicResults =
            [
                new SerpOrganicResult
                {
                    Position = 1,
                    Url = "https://local-org.example/",
                    Title = "Local",
                    Snippet = "",
                    Domain = "local-org.example",
                },
            ],
            LocalPlaces = [new SerpLocalPlace("maps-place.example", 26.4, -80.1)],
            Features = new SerpFeatures(),
            FetchedAt = DateTimeOffset.UtcNow,
        };

        var domains = PillarDemandEnricher.CollectLocalCompetitorDomains(serp, null);

        Assert.Equal(2, domains.Count);
        Assert.Contains("local-org.example", domains);
        Assert.Contains("maps-place.example", domains);
    }
}
