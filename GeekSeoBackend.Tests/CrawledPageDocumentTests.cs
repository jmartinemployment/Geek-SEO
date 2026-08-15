using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class CrawledPageDocumentTests
{
    [Fact]
    public void HasDocument_is_false_for_http_404()
    {
        var page = new CrawledPage("https://example.com/missing", "<h1>404</h1>")
        {
            StatusCode = 404,
            SoftNotFound = true,
        };

        Assert.False(page.HasDocument);
    }

    [Fact]
    public void HasDocument_is_false_for_soft_404_with_http_200()
    {
        var page = new CrawledPage(
            "https://example.com/gone",
            "<h1>404</h1><h2>This page could not be found.</h2>")
        {
            StatusCode = 200,
            SoftNotFound = true,
        };

        Assert.False(page.HasDocument);
    }

    [Fact]
    public void HasDocument_is_true_for_2xx_real_page()
    {
        var page = new CrawledPage("https://example.com/", "<h1>Home</h1>") { StatusCode = 200 };

        Assert.True(page.HasDocument);
    }

    [Fact]
    public void ToSiteStructureWrite_does_not_store_404_chrome_as_page_context()
    {
        var html = """
            <html><head><title>404</title></head><body>
            <h1>404</h1>
            <h2>This page could not be found.</h2>
            </body></html>
            """;
        var crawl = new SiteCrawlData(
            [
                new CrawledPage("https://example.com/missing", html)
                {
                    StatusCode = 404,
                    SoftNotFound = true,
                    FinalUrl = "https://example.com/missing",
                },
            ],
            1,
            1);

        var write = SiteAnalyzerStepRelationalLoader.ToSiteStructureWrite(
            crawl,
            SiteAnalyzerStepRelationalLoader.EmptyInternalLinks(1),
            SiteAnalyzerStepRelationalLoader.EmptyUrlPatterns());

        var stored = Assert.Single(write.Pages);
        Assert.Equal(404, stored.StatusCode);
        Assert.Empty(stored.ContextData?.Headings ?? []);
        Assert.Equal("", stored.ContextData?.MainContentMarkdown ?? "");
        Assert.DoesNotContain("This page could not be found", stored.VisibleText);
    }
}
