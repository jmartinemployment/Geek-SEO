using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteExtraction;

public sealed class SitemapExtractor(IHttpClientFactory factory, ILogger<SitemapExtractor> logger)
{
    private static readonly string[] SitemapPaths =
    [
        "/sitemap.xml", "/sitemap_index.xml",
        "/sitemap-pages.xml", "/sitemap-services.xml", "/sitemap-locations.xml",
        "/sitemap-posts.xml",
    ];

    public async Task<SitemapData> ExtractAsync(string siteUrl, CancellationToken ct)
    {
        var client = BuildClient();
        var urls = await FetchUrlsAsync(siteUrl, client, ct);
        var pillars = GroupIntoPillars(urls, siteUrl);
        if (urls.Count > 0 && pillars.Count == 0)
        {
            logger.LogInformation(
                "Sitemap for {Url} lists {UrlCount} URL(s) with no interior paths — pillar discovery will use schema.org and other signals",
                siteUrl, urls.Count);
        }

        return new SitemapData(pillars, urls.Count, urls);
    }

    private async Task<List<string>> FetchUrlsAsync(string siteUrl, HttpClient client, CancellationToken ct)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var all = new List<string>();

        foreach (var path in SitemapPaths)
        {
            var candidate = siteUrl.TrimEnd('/') + path;
            all.AddRange(await TryFetchSitemapAsync(candidate, client, 0, visited, ct));
        }

        string? robots = null;
        try
        {
            var robotsUrl = siteUrl.TrimEnd('/') + "/robots.txt";
            robots = await client.GetStringAsync(robotsUrl, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // robots.txt is optional seed input for sitemap URLs.
        }

        if (robots is not null)
        {
            var parsed = RobotsTxt.Parse(robots);
            foreach (var sitemapUrl in parsed.Sitemaps)
                all.AddRange(await TryFetchSitemapAsync(sitemapUrl, client, 0, visited, ct));
        }

        return all.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private const int MaxSitemapIndexDepth = 5;

    private async Task<List<string>> TryFetchSitemapAsync(
        string url, HttpClient client, int depth, HashSet<string> visited, CancellationToken ct)
    {
        if (depth > MaxSitemapIndexDepth)
            return [];
        if (!visited.Add(url))
            return [];

        string content;
        try
        {
            content = await client.GetStringAsync(url, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return [];
        }

        if (!TryParseSitemapDocument(content, out var doc) || doc?.Root is null)
        {
            logger.LogWarning("Sitemap at {Url} is not usable XML and is not crawl input", url);
            return [];
        }

        var ns = doc.Root.Name.Namespace;

        if (doc.Root.Name.LocalName == "sitemapindex")
        {
            var urls = new List<string>();
            var childUrls = doc.Descendants(ns + "loc")
                .Select(e => e.Value.Trim())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToList();

            foreach (var childUrl in childUrls)
                urls.AddRange(await TryFetchSitemapAsync(childUrl, client, depth + 1, visited, ct));
            return urls;
        }

        return doc.Descendants(ns + "loc")
            .Select(e => e.Value.Trim())
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();
    }

    private static List<DiscoveredPillar> GroupIntoPillars(List<string> absoluteUrls, string siteUrl)
    {
        var origin = GetOrigin(siteUrl);
        var pathGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in absoluteUrls)
        {
            if (!TryGetRelativePath(url, origin, out var path)) continue;

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) continue;

            var first = segments[0];

            if (!pathGroups.ContainsKey(first))
                pathGroups[first] = [];
            if (segments.Length > 1)
                pathGroups[first].Add(segments[1]);
        }

        return pathGroups
            .Where(kv => kv.Value.Count > 0)
            .Select(kv => new DiscoveredPillar
            {
                Name = SlugToTitle(kv.Key),
                Slug = kv.Key,
                Intent = InferIntent(kv.Key),
                Source = "sitemap",
                ChildPageCount = kv.Value.Count,
                ChildSlugs = kv.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            })
            .ToList();
    }

    private static string GetOrigin(string siteUrl)
    {
        try
        {
            var uri = new Uri(siteUrl);
            return uri.GetLeftPart(UriPartial.Authority);
        }
        catch
        {
            return siteUrl.TrimEnd('/');
        }
    }

    private static bool TryGetRelativePath(string url, string origin, out string path)
    {
        path = string.Empty;
        if (!url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)) return false;
        path = url[origin.Length..].TrimStart('/');
        return true;
    }

    internal static string SlugToTitle(string slug)
    {
        var words = slug.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    internal static string InferIntent(string segment) => segment.ToLowerInvariant() switch
    {
        "services" or "solutions" or "products" or "offerings" => "commercial",
        "locations" or "areas" or "cities" or "regions" or "service-areas" => "local",
        "blog" or "resources" or "guides" or "articles" or "news" or "learn" => "informational",
        _ => "commercial",
    };

    /// <summary>
    /// A sitemap that is not valid XML is not crawl input. Never throw.
    /// </summary>
    internal static bool TryParseSitemapDocument(string content, out XDocument? doc)
    {
        doc = null;
        if (string.IsNullOrWhiteSpace(content))
            return false;
        try
        {
            doc = XDocument.Parse(content);
            return doc.Root is not null;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private HttpClient BuildClient()
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.Add("User-Agent", CrawlerIdentity.UserAgent);
        return client;
    }
}
