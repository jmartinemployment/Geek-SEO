using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class SitePageCrawlerFilterTests
{
    // Utility pages (/about, /contact, /faq, ...) must still be crawled for sitemap inventory /
    // future Site Audit — only pillar/topic selection excludes them via NoisePaths. The crawl's
    // own skip filter is hard-junk-only (assets, wp-admin, login/cart/checkout, feed, search, CDN).
    [Theory]
    [InlineData("https://example.com/about")]
    [InlineData("https://example.com/contact")]
    [InlineData("https://example.com/faq")]
    [InlineData("https://example.com/privacy-policy")]
    [InlineData("https://example.com/services/accounting")]
    public void ExtractSameOriginLinks_keeps_utility_and_content_pages(string url)
    {
        var html = $"<a href=\"{url}\">link</a>";
        var links = SitePageCrawler.ExtractSameOriginLinks(html, "https://example.com/", "https://example.com").ToList();

        Assert.Contains(url, links);
    }

    [Theory]
    [InlineData("https://example.com/cart")]
    [InlineData("https://example.com/checkout")]
    [InlineData("https://example.com/login")]
    [InlineData("https://example.com/wp-admin/edit")]
    [InlineData("https://example.com/feed")]
    [InlineData("https://example.com/search")]
    [InlineData("https://example.com/logo.png")]
    [InlineData("https://example.com/styles.css")]
    public void ExtractSameOriginLinks_skips_hard_junk_only(string url)
    {
        var html = $"<a href=\"{url}\">link</a>";
        var links = SitePageCrawler.ExtractSameOriginLinks(html, "https://example.com/", "https://example.com").ToList();

        Assert.DoesNotContain(url, links);
    }

    [Fact]
    public void ExtractSameOriginLinks_skips_cross_origin_links()
    {
        var html = "<a href=\"https://other-domain.com/page\">link</a>";
        var links = SitePageCrawler.ExtractSameOriginLinks(html, "https://example.com/", "https://example.com").ToList();

        Assert.Empty(links);
    }
}
