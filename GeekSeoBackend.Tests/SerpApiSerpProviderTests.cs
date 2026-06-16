using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Providers.Seo.SerpApi;

namespace GeekSeoBackend.Tests;

public sealed class SerpApiSerpProviderTests
{
    [Fact]
    public void ParseResponse_ReadsOrganicPaaAndRelatedSearches()
    {
        const string raw = """
            {
              "search_metadata": { "status": "Success" },
              "organic_results": [
                {
                  "position": 1,
                  "title": "Best CRM",
                  "link": "https://example.com/crm",
                  "snippet": "Compare tools",
                  "displayed_link": "example.com"
                }
              ],
              "related_questions": [
                { "question": "What is the best CRM?", "snippet": "It depends." }
              ],
              "related_searches": [
                { "query": "crm for startups" },
                "free crm software"
              ],
              "answer_box": { "snippet": "CRM helps manage customers." },
              "local_results": [
                {
                  "title": "Local CRM Co",
                  "links": { "website": "https://localcrm.example" }
                }
              ],
              "knowledge_graph": {}
            }
            """;

        var request = new SerpRequest
        {
            Keyword = "best crm",
            Location = "United States",
            LanguageCode = "en",
            CountryCode = "US",
            ResultCount = 10,
        };

        var result = SerpApiSerpProvider.ParseResponse(request, raw);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.OrganicResults);
        Assert.Equal("https://example.com/crm", result.Value.OrganicResults[0].Url);
        Assert.Equal("example.com", result.Value.OrganicResults[0].Domain);
        Assert.Single(result.Value.PeopleAlsoAsk);
        Assert.Equal(2, result.Value.RelatedSearches.Count);
        Assert.Equal("CRM helps manage customers.", result.Value.FeaturedSnippetText);
        Assert.True(result.Value.Features.HasPeopleAlsoAsk);
        Assert.True(result.Value.Features.HasFeaturedSnippet);
        Assert.True(result.Value.Features.HasLocalPack);
        Assert.True(result.Value.Features.HasKnowledgePanel);
        Assert.Single(result.Value.LocalPlaceDomains);
        Assert.Equal("localcrm.example", result.Value.LocalPlaceDomains[0]);
    }

    [Fact]
    public void ParseResponse_FailsWhenOrganicMissing()
    {
        const string raw = """
            {
              "search_metadata": { "status": "Success" },
              "organic_results": []
            }
            """;

        var request = new SerpRequest
        {
            Keyword = "test",
            Location = "United States",
            LanguageCode = "en",
            ResultCount = 10,
        };

        var result = SerpApiSerpProvider.ParseResponse(request, raw);

        Assert.False(result.IsSuccess);
        Assert.Contains("no organic", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseResponse_FailsOnSerpApiErrorField()
    {
        const string raw = """{ "error": "Invalid API key." }""";

        var request = new SerpRequest
        {
            Keyword = "test",
            Location = "United States",
            LanguageCode = "en",
            ResultCount = 10,
        };

        var result = SerpApiSerpProvider.ParseResponse(request, raw);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid API key", result.Error, StringComparison.Ordinal);
    }
}
