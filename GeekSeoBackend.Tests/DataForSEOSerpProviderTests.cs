using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Providers.Seo;

namespace GeekSeoBackend.Tests;

public sealed class DataForSEOSerpProviderTests
{
    [Fact]
    public void ParseResponse_ReadsOrganicPaaAndStringRelatedSearches()
    {
        const string raw = """
            {
              "status_code": 20000,
              "tasks": [{
                "result": [{
                  "items": [
                    {
                      "type": "organic",
                      "rank_group": 1,
                      "url": "https://example.com/crm",
                      "title": "Best CRM",
                      "description": "Compare tools",
                      "domain": "example.com"
                    },
                    {
                      "type": "people_also_ask",
                      "items": [
                        { "title": "What is the best CRM?" }
                      ]
                    },
                    {
                      "type": "related_searches",
                      "items": [
                        "crm for startups",
                        { "title": "free crm software" }
                      ]
                    }
                  ]
                }]
              }]
            }
            """;

        var request = new SerpRequest
        {
            Keyword = "best crm",
            Location = "United States",
            LanguageCode = "en",
            ResultCount = 50,
        };

        var result = DataForSEOSerpProvider.ParseResponse(request, raw);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.OrganicResults);
        Assert.Equal("https://example.com/crm", result.Value.OrganicResults[0].Url);
        Assert.Single(result.Value.PeopleAlsoAsk);
        Assert.Equal(2, result.Value.RelatedSearches.Count);
        Assert.True(result.Value.Features.HasPeopleAlsoAsk);
    }

    [Fact]
    public void ParseResponse_FailsWhenItemsMissing()
    {
        const string raw = """
            {
              "status_code": 20000,
              "tasks": [{ "result": [{}] }]
            }
            """;

        var request = new SerpRequest
        {
            Keyword = "test",
            Location = "United States",
            LanguageCode = "en",
            ResultCount = 10,
        };

        var result = DataForSEOSerpProvider.ParseResponse(request, raw);

        Assert.False(result.IsSuccess);
        Assert.Contains("no SERP items", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
