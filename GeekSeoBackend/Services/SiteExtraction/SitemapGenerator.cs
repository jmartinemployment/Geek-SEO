using System.Text;
using System.Xml.Linq;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Site Analyzer step 1: same-origin BFS from the homepage. A public sitemap is optional seed
/// input, not a requirement and not a fallback. Inventory is pages that returned 2xx.
/// Throws when zero pages were fetched.
/// </summary>
public sealed class SitemapGenerator(
    SitemapExtractor sitemapExtractor,
    SitePageCrawler sitePageCrawler,
    ILogger<SitemapGenerator> logger)
{
    public sealed record GeneratedSitemap(
        IReadOnlyList<SitemapInventoryUrl> Inventory,
        string XmlDocument);

    public sealed record SitemapInventoryUrl(string Url, string SourceType);

    /// <summary>
    /// Crawls from the homepage, using any usable public sitemap URLs as extra seeds.
    /// Inventory is 2xx fetches only. Unfetched sitemap locs are not inventory.
    /// </summary>
    public async Task<GeneratedSitemap> GenerateAsync(string domain, IBrowser? browser, CancellationToken ct)
    {
        if (!TryGetOrigin(domain, out var origin))
            throw new InvalidOperationException($"Sitemap generation found no pages for {domain} — invalid site URL.");

        var publicSitemap = await sitemapExtractor.ExtractAsync(domain, ct);
        var sitemapSeeds = publicSitemap.SampleUrls
            .Where(u => IsSameOrigin(u, origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var crawl = await sitePageCrawler.CrawlAsync(domain, sitemapSeeds, browser, ct);
        var inventory = InventoryFromFetchedPages(sitemapSeeds, crawl.Pages, origin);

        if (inventory.Count == 0)
        {
            throw new InvalidOperationException(
                $"Sitemap generation fetched no pages for {domain}. The site may be unreachable, " +
                "block this crawler, or Playwright/Chromium is unavailable.");
        }

        var sitemapUrlCount = inventory.Count(i => i.SourceType == "sitemap");
        logger.LogInformation(
            "Sitemap generator for {Domain}: {Fetched} fetched page(s) ({SitemapCount} also listed in public sitemap)",
            domain,
            inventory.Count,
            sitemapUrlCount);

        var xml = BuildUrlsetXml(inventory.Select(i => i.Url));
        return new GeneratedSitemap(inventory, xml);
    }

    /// <summary>
    /// 2xx same-origin fetches. SourceType is sitemap when that URL was a public-sitemap seed.
    /// </summary>
    internal static IReadOnlyList<SitemapInventoryUrl> InventoryFromFetchedPages(
        IReadOnlyList<string> sitemapSeeds,
        IReadOnlyList<CrawledPage> pages,
        string origin)
    {
        var seedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in sitemapSeeds)
        {
            if (IsSameOrigin(seed, origin))
                seedKeys.Add(CrawlUrl.Canonicalize(seed));
        }

        var inventory = new List<SitemapInventoryUrl>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            if (page.StatusCode is < 200 or >= 300)
                continue;
            var url = page.FinalUrl.Length > 0 ? page.FinalUrl : page.Url;
            if (!IsSameOrigin(url, origin))
                continue;
            var key = CrawlUrl.Canonicalize(url);
            if (!seen.Add(key))
                continue;
            inventory.Add(new SitemapInventoryUrl(key, seedKeys.Contains(key) ? "sitemap" : "generated"));
        }

        return inventory;
    }

    /// <summary>Builds a standard sitemap.xml urlset document from a URL inventory.</summary>
    public static string BuildUrlsetXml(IEnumerable<string> urls)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlset = new XElement(
            ns + "urlset",
            urls
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(u => new XElement(ns + "url", new XElement(ns + "loc", u))));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), urlset);
        using var writer = new StringWriter();
        doc.Save(writer, SaveOptions.None);
        return writer.ToString();
    }

    private static bool TryGetOrigin(string siteUrl, out string origin)
    {
        origin = string.Empty;
        try
        {
            var uri = new Uri(SiteUrlNormalizer.Normalize(siteUrl));
            origin = uri.GetLeftPart(UriPartial.Authority);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSameOrigin(string url, string origin)
    {
        try
        {
            return CrawlUrl.IsSameSite(new Uri(url), new Uri(origin));
        }
        catch
        {
            return false;
        }
    }
}
