using System.Text.RegularExpressions;
using System.Xml.Linq;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

public sealed class SitemapExtractor(IHttpClientFactory factory, ILogger<SitemapExtractor> logger)
{
    private const int MaxUrls = 5_000;
    private const int MaxChildSitemaps = 3;

    private static readonly string[] SitemapPaths =
    [
        "/sitemap.xml", "/sitemap_index.xml",
        "/sitemap-pages.xml", "/sitemap-services.xml", "/sitemap-locations.xml",
        "/sitemap-posts.xml",
    ];

    public async Task<SitemapData> ExtractAsync(string siteUrl, CancellationToken ct)
    {
        try
        {
            var client = BuildClient();
            var urls = await FetchUrlsAsync(siteUrl, client, ct);
            var pillars = GroupIntoPillars(urls, siteUrl);
            return new SitemapData(pillars, urls.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sitemap extraction failed for {Url}", siteUrl);
            return new SitemapData([], 0);
        }
    }

    private async Task<List<string>> FetchUrlsAsync(string siteUrl, HttpClient client, CancellationToken ct)
    {
        // Try known sitemap paths first
        foreach (var path in SitemapPaths)
        {
            var candidate = siteUrl.TrimEnd('/') + path;
            var urls = await TryFetchSitemapAsync(candidate, client, 0, ct);
            if (urls.Count > 0) return urls;
        }

        // Try robots.txt
        try
        {
            var robotsUrl = siteUrl.TrimEnd('/') + "/robots.txt";
            var robots = await client.GetStringAsync(robotsUrl, ct);
            foreach (var line in robots.Split('\n'))
            {
                if (!line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase)) continue;
                var sitemapUrl = line["Sitemap:".Length..].Trim();
                var urls = await TryFetchSitemapAsync(sitemapUrl, client, 0, ct);
                if (urls.Count > 0) return urls;
            }
        }
        catch
        {
            // robots.txt unavailable — ignore
        }

        return [];
    }

    private async Task<List<string>> TryFetchSitemapAsync(string url, HttpClient client, int depth, CancellationToken ct)
    {
        try
        {
            var content = await client.GetStringAsync(url, ct);
            var doc = XDocument.Parse(content);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            // Sitemap index — fetch first N child sitemaps
            if (doc.Root?.Name.LocalName == "sitemapindex")
            {
                var urls = new List<string>();
                var childUrls = doc.Descendants(ns + "loc")
                    .Select(e => e.Value.Trim())
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Take(MaxChildSitemaps)
                    .ToList();

                foreach (var childUrl in childUrls)
                {
                    var childUrls2 = await TryFetchSitemapAsync(childUrl, client, depth + 1, ct);
                    urls.AddRange(childUrls2);
                    if (urls.Count >= MaxUrls) break;
                }
                return urls.Take(MaxUrls).ToList();
            }

            // Regular sitemap
            return doc.Descendants(ns + "loc")
                .Select(e => e.Value.Trim())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Take(MaxUrls)
                .ToList();
        }
        catch
        {
            return [];
        }
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
            if (NoisePaths.IsNoise(first)) continue;

            if (!pathGroups.ContainsKey(first))
                pathGroups[first] = [];
            if (segments.Length > 1 && !NoisePaths.IsNoise(segments[1]))
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
                ChildSlugs = kv.Value.Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList(),
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

    private HttpClient BuildClient()
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)");
        return client;
    }
}
