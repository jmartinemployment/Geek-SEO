using System.Xml.Linq;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class SitemapGeneratorXmlTests
{
    [Fact]
    public void BuildUrlsetXml_produces_standard_urlset_with_one_loc_per_url()
    {
        var xml = SitemapGenerator.BuildUrlsetXml(
        [
            "https://example.com/",
            "https://example.com/about",
            "https://example.com/services",
        ]);

        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        Assert.Equal(ns + "urlset", doc.Root!.Name);
        var locs = doc.Descendants(ns + "loc").Select(e => e.Value).ToList();
        Assert.Equal(3, locs.Count);
        Assert.Contains("https://example.com/", locs);
        Assert.Contains("https://example.com/about", locs);
        Assert.Contains("https://example.com/services", locs);
    }

    [Fact]
    public void BuildUrlsetXml_dedupes_case_insensitively_and_drops_blank_entries()
    {
        var xml = SitemapGenerator.BuildUrlsetXml(
        [
            "https://example.com/about",
            "https://EXAMPLE.com/ABOUT",
            "",
            "   ",
        ]);

        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locs = doc.Descendants(ns + "loc").Select(e => e.Value).ToList();

        Assert.Single(locs);
    }

    [Fact]
    public void BuildUrlsetXml_on_empty_inventory_produces_empty_urlset()
    {
        // Not the vetoed "empty is success" path — this only verifies the XML builder itself is
        // well-formed on an empty list; the generator throws before ever calling this with an
        // empty inventory (see SitemapGenerator.GenerateAsync's zero-URL fail-closed check).
        var xml = SitemapGenerator.BuildUrlsetXml([]);
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        Assert.Empty(doc.Descendants(ns + "url"));
    }

    [Fact]
    public void InventoryFromFetchedPages_uses_2xx_fetches_only_not_unfetched_sitemap_locs()
    {
        var seeds = new[]
        {
            "https://example.com/listed-but-not-fetched",
            "https://example.com/",
        };
        var pages = new[]
        {
            new CrawledPage("https://example.com/", "<html/>") { StatusCode = 200, FinalUrl = "https://example.com/" },
            new CrawledPage("https://example.com/gone", "") { StatusCode = 404, FinalUrl = "https://example.com/gone" },
        };

        var inventory = SitemapGenerator.InventoryFromFetchedPages(seeds, pages, "https://example.com");

        Assert.Single(inventory);
        Assert.Equal("https://example.com/", inventory[0].Url);
        Assert.Equal("sitemap", inventory[0].SourceType);
    }

    [Fact]
    public void InventoryFromFetchedPages_marks_unlisted_2xx_as_generated()
    {
        var pages = new[]
        {
            new CrawledPage("https://example.com/about", "<html/>")
            {
                StatusCode = 200,
                FinalUrl = "https://example.com/about",
            },
        };

        var inventory = SitemapGenerator.InventoryFromFetchedPages([], pages, "https://example.com");

        Assert.Single(inventory);
        Assert.Equal("generated", inventory[0].SourceType);
    }
}
