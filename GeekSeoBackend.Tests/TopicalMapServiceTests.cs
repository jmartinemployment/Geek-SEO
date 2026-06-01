using GeekSeoBackend.Models;
using GeekSeoBackend.Services;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class TopicalMapServiceTests
{
    private const string Home = "https://www.geekatyourspot.com/";
    private const string Services = "https://www.geekatyourspot.com/services/web-design/";

    [Fact]
    public void BuildTopics_MergesSimilarQueries_WithOrderIndependentClusterKey()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("web design boynton beach", Home, 120),
            Row("boynton beach web design", Home, 95),
            Row("website design boynton beach fl", Home, 40),
        };

        var topics = TopicalMapService.BuildTopics(rows, []);

        Assert.Single(topics);
        Assert.Equal(255, topics[0].TotalImpressions);
        Assert.True(topics[0].Queries.Count >= 2);
        Assert.Equal("partial", topics[0].Coverage);
        Assert.Equal("Web Design Boynton Beach", topics[0].Name);
    }

    [Fact]
    public void BuildTopics_KeepsDistinctHomepageIntents_AsSeparatePartialTopics()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("it support boynton beach", Home, 80),
            Row("computer repair boynton beach", Home, 70),
            Row("tech support delray beach", Home, 60),
            Row("managed it services palm beach", Home, 50),
        };

        var topics = TopicalMapService.BuildTopics(rows, []);

        Assert.Equal(4, topics.Count);
        Assert.All(topics, t =>
        {
            Assert.Equal("partial", t.Coverage);
            Assert.Equal(Normalize(Home), Normalize(t.MatchedPageUrl!));
            Assert.Equal("gsc", t.MatchSource);
        });
    }

    [Fact]
    public void BuildTopics_MarksDedicatedPage_AsCovered()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("web design boynton beach", Services, 200, position: 6),
            Row("boynton beach web designer", Services, 80, position: 9),
        };

        var topics = TopicalMapService.BuildTopics(rows, []);

        Assert.Single(topics);
        Assert.Equal("covered", topics[0].Coverage);
        Assert.Equal(Normalize(Services), Normalize(topics[0].MatchedPageUrl!));
        Assert.Equal("Web Design", topics[0].Name);
    }

    [Fact]
    public void BuildTopics_UsesDocumentMatch_WhenNoGscPage()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("cloud migration checklist", "", 90),
        };
        var docs = new List<SeoContentDocument>
        {
            new()
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ProjectId = Guid.NewGuid(),
                Title = "Cloud Migration Guide",
                TargetKeyword = "cloud migration checklist",
                SeoScore = 72,
            },
        };

        var topics = TopicalMapService.BuildTopics(rows, docs);

        Assert.Single(topics);
        Assert.Equal("covered", topics[0].Coverage);
        Assert.Equal("document", topics[0].MatchSource);
        Assert.Equal("Cloud Migration Guide", topics[0].MatchedDocumentTitle);
    }

    [Fact]
    public void BuildTopics_SplitsTopics_WhenLandingPagesDiffer()
    {
        var rows = new List<GoogleRankingRow>
        {
            Row("web design boynton beach", Services, 150, position: 7),
            Row("seo services florida", "https://www.geekatyourspot.com/services/seo/", 120, position: 8),
        };

        var topics = TopicalMapService.BuildTopics(rows, []);

        Assert.Equal(2, topics.Count);
        Assert.All(topics, t => Assert.Equal("covered", t.Coverage));
    }

    private static string Normalize(string url) => url.TrimEnd('/');

    private static GoogleRankingRow Row(
        string query,
        string page,
        long impressions,
        double position = 15) =>
        new()
        {
            Query = query,
            Page = page,
            Impressions = impressions,
            Clicks = Math.Max(1, impressions / 20),
            Position = position,
        };
}
