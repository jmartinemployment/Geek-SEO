using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class CanonicalPageConsolidatorTests
{
    private static CrawledPage Page(string url, string canonical, string html = "<h1>Home</h1>") =>
        new(url, html) { Canonical = canonical, StatusCode = 200 };

    [Fact]
    public void Www_and_apex_declaring_one_canonical_collapse_to_a_single_page()
    {
        var pages = new List<CrawledPage>
        {
            Page("https://www.geekatyourspot.com/", "https://www.geekatyourspot.com"),
            Page("https://geekatyourspot.com/", "https://www.geekatyourspot.com"),
        };

        var result = CanonicalPageConsolidator.Consolidate(pages);

        var only = Assert.Single(result);
        // Uri normalization gives the root path an explicit slash.
        Assert.Equal("https://www.geekatyourspot.com/", only.Url);
    }

    [Fact]
    public void Page_fetched_at_the_canonical_url_wins()
    {
        var pages = new List<CrawledPage>
        {
            Page("https://geekatyourspot.com/", "https://www.geekatyourspot.com", "<h1>apex</h1>"),
            Page("https://www.geekatyourspot.com", "https://www.geekatyourspot.com", "<h1>www</h1>"),
        };

        var only = Assert.Single(CanonicalPageConsolidator.Consolidate(pages));
        Assert.Equal("<h1>www</h1>", only.Html);
    }

    [Fact]
    public void Distinct_documents_are_not_merged()
    {
        var pages = new List<CrawledPage>
        {
            Page("https://www.geekatyourspot.com/", "https://www.geekatyourspot.com"),
            Page("https://www.geekatyourspot.com/use-cases/marketing/ai-marketing-systems",
                 "https://www.geekatyourspot.com/use-cases/marketing/ai-marketing-systems"),
        };

        Assert.Equal(2, CanonicalPageConsolidator.Consolidate(pages).Count);
    }

    [Fact]
    public void Cross_site_canonical_is_ignored()
    {
        // A page cannot hand its identity to another domain inside this crawl.
        var pages = new List<CrawledPage>
        {
            Page("https://www.geekatyourspot.com/a", "https://someone-else.com/a"),
            Page("https://www.geekatyourspot.com/b", "https://someone-else.com/a"),
        };

        var result = CanonicalPageConsolidator.Consolidate(pages);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Url.EndsWith("/a", StringComparison.Ordinal));
        Assert.Contains(result, p => p.Url.EndsWith("/b", StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_canonical_falls_back_to_the_pages_own_url()
    {
        var pages = new List<CrawledPage> { Page("https://www.geekatyourspot.com/x", "") };
        Assert.Equal("https://www.geekatyourspot.com/x", Assert.Single(CanonicalPageConsolidator.Consolidate(pages)).Url);
    }

    [Fact]
    public void Trailing_slash_variants_are_one_page()
    {
        var pages = new List<CrawledPage>
        {
            Page("https://www.geekatyourspot.com/tools", "https://www.geekatyourspot.com/tools"),
            Page("https://www.geekatyourspot.com/tools/", "https://www.geekatyourspot.com/tools/"),
        };

        Assert.Single(CanonicalPageConsolidator.Consolidate(pages));
    }
}
