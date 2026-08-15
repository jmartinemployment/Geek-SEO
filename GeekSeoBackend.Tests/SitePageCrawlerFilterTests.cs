using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class SitePageCrawlerFilterTests
{
    // Every discovered same-origin link gets fetched — nothing is pre-filtered.
    [Theory]
    [InlineData("https://example.com/about")]
    [InlineData("https://example.com/contact")]
    [InlineData("https://example.com/faq")]
    [InlineData("https://example.com/cart")]
    [InlineData("https://example.com/login")]
    [InlineData("https://example.com/feed")]
    [InlineData("https://example.com/logo.png")]
    [InlineData("https://example.com/doc.pdf")]
    [InlineData("https://example.com/styles.css")]
    [InlineData("https://example.com/app.js")]
    [InlineData("https://example.com/font.woff2")]
    [InlineData("https://example.com/archive.zip")]
    [InlineData("https://example.com/privacy-policy")]
    [InlineData("https://example.com/services/accounting")]
    public void ExtractSameOriginLinks_includes_all_same_origin_urls(string url)
    {
        var html = $"<a href=\"{url}\">link</a>";
        var links = SitePageCrawler.ExtractSameOriginLinks(html, "https://example.com/", "https://example.com").ToList();

        Assert.Contains(url, links);
    }

    [Fact]
    public void ExtractSameOriginLinks_treats_www_and_apex_as_same_site()
    {
        var html = "<a href=\"https://www.example.com/page\">link</a>";
        var links = SitePageCrawler.ExtractSameOriginLinks(html, "https://example.com/", "https://example.com").ToList();

        Assert.Contains("https://www.example.com/page", links);
    }

    [Fact]
    public void ExtractSameOriginLinks_skips_cross_origin_links()
    {
        var html = "<a href=\"https://other-domain.com/page\">link</a>";
        var links = SitePageCrawler.ExtractSameOriginLinks(html, "https://example.com/", "https://example.com").ToList();

        Assert.Empty(links);
    }
}
