using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class UrlPageKeywordResolverTests
{
    [Fact]
    public void Derive_PrefersMetaTitle()
    {
        var page = new PageContent
        {
            Url = "https://example.com/blog/quickbooks-automation",
            FullText = "content",
            MetaTitle = "QuickBooks automation for SMBs | Example Co",
            Headings = [new PageHeading { Level = 1, Text = "H1 fallback" }],
            CrawledAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal("QuickBooks automation for SMBs", UrlPageKeywordResolver.Derive(page, page.Url));
    }

    [Fact]
    public void Derive_FallsBackToH1()
    {
        var page = new PageContent
        {
            Url = "https://example.com/blog/quickbooks-automation",
            FullText = "content",
            Headings = [new PageHeading { Level = 1, Text = "QuickBooks automation guide" }],
            CrawledAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal("QuickBooks automation guide", UrlPageKeywordResolver.Derive(page, page.Url));
    }

    [Fact]
    public void Derive_FallsBackToUrlSlug()
    {
        var page = new PageContent
        {
            Url = "https://example.com/blog/quickbooks-automation",
            FullText = "content",
            CrawledAt = DateTimeOffset.UtcNow,
        };

        Assert.Equal("Quickbooks Automation", UrlPageKeywordResolver.Derive(page, page.Url));
    }

    [Fact]
    public void NormalizeUrl_AddsHttpsWhenMissing()
    {
        Assert.Equal("https://example.com/page", UrlPageKeywordResolver.NormalizeUrl("example.com/page"));
    }
}
