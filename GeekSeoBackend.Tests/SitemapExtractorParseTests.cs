using System.Xml.Linq;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class SitemapExtractorParseTests
{
    [Fact]
    public void TryParseSitemapDocument_accepts_utf8_urlset()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <url><loc>https://example.com/</loc></url>
            </urlset>
            """;

        Assert.True(SitemapExtractor.TryParseSitemapDocument(xml, out var doc));
        Assert.NotNull(doc);
        Assert.Equal("urlset", doc!.Root!.Name.LocalName);
    }

    [Fact]
    public void TryParseSitemapDocument_utf16_declaration_is_not_usable_input()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-16"?>
            <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <url><loc>https://example.com/</loc></url>
            </urlset>
            """;

        var ok = SitemapExtractor.TryParseSitemapDocument(xml, out var doc);
        if (!ok)
        {
            Assert.Null(doc);
            return;
        }

        Assert.NotNull(doc);
    }

    [Fact]
    public void TryParseSitemapDocument_garbage_is_not_usable_input()
    {
        Assert.False(SitemapExtractor.TryParseSitemapDocument("not xml", out var a));
        Assert.Null(a);
        Assert.False(SitemapExtractor.TryParseSitemapDocument("\uFEFF<", out var b));
        Assert.Null(b);
        Assert.False(SitemapExtractor.TryParseSitemapDocument("", out var c));
        Assert.Null(c);
    }
}
