using GeekSeoBackend.Providers.Seo.SerpApi;

namespace GeekSeoBackend.Tests;

public sealed class SerpApiRankSnapshotProviderTests
{
    [Fact]
    public void ParseResponse_FindsMatchingDomain()
    {
        const string raw = """
            {
              "search_metadata": { "status": "Success" },
              "organic_results": [
                {
                  "position": 3,
                  "link": "https://www.example.com/page",
                  "title": "Example"
                },
                {
                  "position": 1,
                  "link": "https://other.com/",
                  "title": "Other"
                }
              ]
            }
            """;

        var result = SerpApiRankSnapshotProvider.ParseResponse("widgets", "example.com", raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Position);
        Assert.Equal("https://www.example.com/page", result.Value.PageUrl);
    }

    [Fact]
    public void ParseResponse_ReturnsNullPositionWhenDomainNotFound()
    {
        const string raw = """
            {
              "search_metadata": { "status": "Success" },
              "organic_results": [
                { "position": 1, "link": "https://other.com/", "title": "Other" }
              ]
            }
            """;

        var result = SerpApiRankSnapshotProvider.ParseResponse("widgets", "example.com", raw);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Position);
        Assert.Null(result.Value.PageUrl);
    }
}
