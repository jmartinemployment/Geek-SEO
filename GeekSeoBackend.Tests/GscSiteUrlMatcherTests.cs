using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class GscSiteUrlMatcherTests
{
    [Fact]
    public void Match_ReturnsExactAccessibleUrl_WhenTrailingSlashDiffers()
    {
        var accessible = new[] { "https://www.geekatyourspot.com/" };
        var matched = GscSiteUrlMatcher.Match(accessible, "https://www.geekatyourspot.com");

        Assert.Equal("https://www.geekatyourspot.com/", matched);
    }

    [Fact]
    public void Match_ReturnsExactAccessibleUrl_WhenWwwDiffers()
    {
        var accessible = new[] { "https://www.geekatyourspot.com/", "sc-domain:geekatyourspot.com" };
        var matched = GscSiteUrlMatcher.Match(accessible, "https://geekatyourspot.com");

        Assert.Equal("https://www.geekatyourspot.com/", matched);
    }

    [Fact]
    public void Match_ReturnsNull_WhenNoAccessibleSites()
    {
        Assert.Null(GscSiteUrlMatcher.Match([], "https://www.example.com/"));
    }

    [Fact]
    public void SitesEquivalent_MatchesDomainPropertyToWwwUrl()
    {
        Assert.True(GscSiteUrlMatcher.SitesEquivalent(
            "sc-domain:geekatyourspot.com",
            "https://www.geekatyourspot.com/"));
    }
}
