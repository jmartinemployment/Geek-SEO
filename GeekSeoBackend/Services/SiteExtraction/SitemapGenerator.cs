using System.Text;
using System.Xml.Linq;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Site Analyzer step 1 (Semrush-style sitemap generator): always-run same-origin BFS crawl from
/// the homepage (unlimited discovery, hard-junk-only skip filter — same crawl rules as
/// <see cref="SitePageCrawler"/>), merged with any public <c>/sitemap.xml</c> URLs from
/// <see cref="SitemapExtractor"/>. Produces the full URL inventory (never a discovery cap) and a
/// standard <c>urlset</c> XML document. Fails closed (throws) when zero URLs are discovered —
/// there is no empty soft-success path.
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
    /// Runs the generator. Throws <see cref="InvalidOperationException"/> when the combined
    /// public-sitemap + crawl-discovered inventory is empty — that is a hard step-1 failure, not
    /// a reason to soft-continue with an empty artifact.
    /// </summary>
    public async Task<GeneratedSitemap> GenerateAsync(string domain, IBrowser? browser, CancellationToken ct)
    {
        if (!TryGetOrigin(domain, out var origin))
            throw new InvalidOperationException($"Sitemap generation found no pages for {domain} — invalid site URL.");

        // Enrichment: any URLs already published in a public /sitemap.xml.
        var publicSitemap = await sitemapExtractor.ExtractAsync(domain, ct);
        var sitemapUrls = publicSitemap.SampleUrls
            .Where(u => IsSameOrigin(u, origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Primary discovery: unlimited same-origin BFS crawl from the homepage. Missing a public
        // sitemap.xml is a normal input condition — the crawl is the source of truth, not a fallback.
        var crawl = await sitePageCrawler.CrawlAsync(domain, sitemapUrls, browser, ct);
        var crawledUrls = crawl.Pages
            .Select(p => p.Url)
            .Where(u => IsSameOrigin(u, origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sitemapSet = new HashSet<string>(sitemapUrls, StringComparer.OrdinalIgnoreCase);
        var inventory = new List<SitemapInventoryUrl>();
        inventory.AddRange(sitemapUrls.Select(u => new SitemapInventoryUrl(u, "sitemap")));
        inventory.AddRange(crawledUrls
            .Where(u => !sitemapSet.Contains(u))
            .Select(u => new SitemapInventoryUrl(u, "generated")));

        if (inventory.Count == 0)
        {
            throw new InvalidOperationException(
                $"Sitemap generation found no pages for {domain}. The site may be unreachable, " +
                "block automated crawlers, or return no fetchable homepage content.");
        }

        logger.LogInformation(
            "Sitemap generator for {Domain}: {SitemapCount} public sitemap URL(s), {CrawlCount} crawl-discovered URL(s), {Total} total inventory URL(s)",
            domain,
            sitemapUrls.Count,
            inventory.Count - sitemapUrls.Count,
            inventory.Count);

        var xml = BuildUrlsetXml(inventory.Select(i => i.Url));
        return new GeneratedSitemap(inventory, xml);
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
            return new Uri(url).GetLeftPart(UriPartial.Authority)
                .Equals(origin, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
