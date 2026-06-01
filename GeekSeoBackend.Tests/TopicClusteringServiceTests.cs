using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;

namespace GeekSeoBackend.Tests;

public sealed class TopicClusteringServiceTests
{
    [Fact]
    public void ClusterGscQueries_GroupsByDedicatedPage_WhenShareIsHigh()
    {
        const string services = "https://www.example.com/services/web-design/";
        var rows = new[]
        {
            Row("web design boynton beach", services, 200, 6),
            Row("boynton beach web designer", services, 80, 9),
        };

        var clusters = TopicClusteringService.ClusterGscQueries(rows);

        Assert.Single(clusters);
        Assert.Equal("gsc_page", clusters[0].ClusterMethod);
        Assert.Equal(280, clusters[0].TotalImpressions);
    }

    [Fact]
    public void ClusterGscQueries_UsesSerpSignature_WhenProvided()
    {
        var rows = new[]
        {
            Row("cloud backup solutions", "", 90, 20),
            Row("best cloud backup", "", 70, 22),
        };
        var serp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cloud backup solutions"] = "https://a.com/x|https://b.com/y|https://c.com/z",
            ["best cloud backup"] = "https://a.com/x|https://b.com/y|https://c.com/z",
        };

        var clusters = TopicClusteringService.ClusterGscQueries(rows, serp);

        Assert.Single(clusters);
        Assert.Equal("serp", clusters[0].ClusterMethod);
    }

    [Fact]
    public void ComputePriorityScore_RanksGapsHigherThanCovered()
    {
        var gap = TopicClusteringService.ComputePriorityScore(100, 10, "gap", 35);
        var covered = TopicClusteringService.ComputePriorityScore(100, 10, "covered", 35);
        Assert.True(gap > covered);
    }

    private static GscQueryRow Row(string query, string page, long impressions, double position) =>
        new(query, page, impressions, Math.Max(1, impressions / 20), position);
}
