using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class RobotsTxtTests
{
    [Fact]
    public void Longest_Allow_beats_shorter_Disallow()
    {
        const string robots = """
            User-agent: *
            Disallow: /fish
            Allow: /fish.html
            """;

        var rules = RobotsTxt.Parse(robots);
        Assert.True(rules.IsAllowed("/fish.html"));
        Assert.False(rules.IsAllowed("/fish/soup"));
    }

    [Fact]
    public void Tie_goes_to_Allow()
    {
        const string robots = """
            User-agent: *
            Disallow: /x
            Allow: /x
            """;

        Assert.True(RobotsTxt.Parse(robots).IsAllowed("/x"));
    }

    [Fact]
    public void Wildcard_and_end_anchor()
    {
        const string robots = """
            User-agent: *
            Disallow: /*.php$
            Allow: /allowed.php$
            """;

        var rules = RobotsTxt.Parse(robots);
        Assert.False(rules.IsAllowed("/page.php"));
        Assert.True(rules.IsAllowed("/page.php?x=1"));
        Assert.True(rules.IsAllowed("/allowed.php"));
    }

    [Fact]
    public void Most_specific_user_agent_group_wins()
    {
        const string robots = """
            User-agent: *
            Disallow: /

            User-agent: GeekSEO
            Disallow: /private
            Allow: /
            """;

        var rules = RobotsTxt.Parse(robots);
        Assert.True(rules.IsAllowed("/public"));
        Assert.False(rules.IsAllowed("/private"));
    }

    [Fact]
    public void Status_4xx_allows_all()
    {
        Assert.True(RobotsTxt.FromStatus(404).IsAllowed("/anything"));
    }

    [Fact]
    public void Status_5xx_disallows_all()
    {
        Assert.False(RobotsTxt.FromStatus(503).IsAllowed("/"));
    }

    [Fact]
    public void Parses_Sitemap_and_Crawl_delay()
    {
        const string robots = """
            User-agent: *
            Disallow: /admin
            Crawl-delay: 2
            Sitemap: https://example.com/sitemap.xml
            Sitemap: https://example.com/news.xml
            """;

        var rules = RobotsTxt.Parse(robots);
        Assert.Equal(2, rules.CrawlDelaySeconds);
        Assert.Equal(2, rules.Sitemaps.Count);
        Assert.False(rules.IsAllowed("/admin"));
        Assert.True(rules.IsAllowed("/"));
    }
}

public sealed class CrawlUrlTests
{
    [Fact]
    public void Strips_utm_and_click_ids_and_sorts_query()
    {
        var canonical = CrawlUrl.Canonicalize(
            "https://Example.COM/Path?b=2&utm_source=x&gclid=abc&a=1");
        Assert.Equal("https://example.com/Path?a=1&b=2", canonical);
    }

    [Fact]
    public void Www_apex_and_scheme_are_same_site()
    {
        Assert.True(CrawlUrl.IsSameSite("https://www.example.com/a", "http://example.com/b"));
        Assert.True(CrawlUrl.IsSameSite("http://example.com/", "https://example.com/"));
        Assert.False(CrawlUrl.IsSameSite("https://example.com/", "https://other.com/"));
    }

    [Fact]
    public void Host_is_case_insensitive_path_is_not()
    {
        var a = CrawlUrl.Canonicalize("https://Example.com/Foo");
        var b = CrawlUrl.Canonicalize("https://example.com/foo");
        Assert.Equal("https://example.com/Foo", a);
        Assert.Equal("https://example.com/foo", b);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Drops_default_ports_and_fragments()
    {
        Assert.Equal(
            "https://example.com/x",
            CrawlUrl.Canonicalize("https://example.com:443/x#section"));
    }
}

public sealed class VisibilityClassifierTests
{
    [Fact]
    public void Visible_at_mobile_is_visible()
    {
        Assert.Equal("visible", VisibilityClassifier.Classify(hiddenAtMobile: false, hiddenAtDesktop: true));
    }

    [Fact]
    public void Hidden_at_both_is_collapsed()
    {
        Assert.Equal("collapsed", VisibilityClassifier.Classify(true, true));
    }

    [Fact]
    public void Hidden_at_mobile_shown_at_desktop_is_desktop_only()
    {
        Assert.Equal("desktop-only", VisibilityClassifier.Classify(true, false));
    }

    [Fact]
    public void ClassifyAll_uses_overlap_when_node_counts_diverge()
    {
        bool[] mobile = [true, false, true];
        bool[] desktop = [true, false];
        var labels = VisibilityClassifier.ClassifyAll(mobile, desktop);
        Assert.Equal(["collapsed", "visible"], labels);
    }
}
