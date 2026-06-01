using GeekSeoBackend.Models;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class CannibalizationServiceTests
{
    [Fact]
    public void BuildIssues_FlagsQueryWithMultipleUrls()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("web design", "https://example.com/a", 50, 8.0),
            Row("web design", "https://example.com/b", 40, 12.0),
            Row("hosting", "https://example.com/c", 100, 5.0),
        };

        var analysis = CannibalizationService.BuildIssues(rows);

        Assert.Single(analysis.Issues);
        Assert.Equal("web design", analysis.Issues[0].Query);
        Assert.Equal(2, analysis.Issues[0].Pages.Count);
        Assert.Equal(90, analysis.Issues[0].TotalImpressions);
        Assert.Equal(2, analysis.UniqueQueryCount);
        Assert.Equal(1, analysis.MultiUrlQueryCount);
    }

    [Fact]
    public void BuildIssues_IgnoresSingleUrlQueries()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("only one page", "https://example.com/a", 200, 3.0),
            Row("split", "https://example.com/x", 5, 10.0),
            Row("split", "https://example.com/y", 5, 11.0),
        };

        var analysis = CannibalizationService.BuildIssues(rows);

        Assert.Single(analysis.Issues);
        Assert.Equal("split", analysis.Issues[0].Query);
        Assert.Equal(2, analysis.UniqueQueryCount);
        Assert.Equal(1, analysis.MultiUrlQueryCount);
    }

    [Fact]
    public void BuildIssues_DoesNotFlagTrailingSlashAndWwwOnlyVariants()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("local seo", "https://www.example.com/services/", 30, 6.0),
            Row("local seo", "https://example.com/services", 25, 7.0),
        };

        var analysis = CannibalizationService.BuildIssues(rows);

        Assert.Empty(analysis.Issues);
        Assert.Equal(0, analysis.MultiUrlQueryCount);
    }

    [Fact]
    public void BuildIssues_ReturnsEmpty_WhenNoRows()
    {
        var analysis = CannibalizationService.BuildIssues([]);
        Assert.Empty(analysis.Issues);
        Assert.Equal(0, analysis.UniqueQueryCount);
        Assert.Equal(0, analysis.MultiUrlQueryCount);
    }

    private static GoogleRankingRow Row(string query, string page, long impressions, double position) => new()
    {
        Query = query,
        Page = page,
        Impressions = impressions,
        Clicks = 0,
        Ctr = 0,
        Position = position,
    };
}
