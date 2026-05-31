using System.Xml.Linq;

namespace GeekSeo.Application.Services.Seo;

public static partial class SiteAuditUrlDiscovery
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };

    public static async Task<IReadOnlyList<string>> DiscoverAsync(
        string siteUrl,
        int maxUrls,
        CancellationToken ct = default)
    {
        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var rootUri))
            return [];

        var cap = Math.Clamp(maxUrls, 1, SiteAuditPageAnalyzer.MaxPagesPerRun);
        var discovered = new List<string> { NormalizeUrl(rootUri) };
        var host = rootUri.Host;

        foreach (var sitemapUrl in await ResolveSitemapUrlsAsync(rootUri, ct))
        {
            if (discovered.Count >= cap)
                break;

            try
            {
                using var response = await Http.GetAsync(sitemapUrl, ct);
                if (!response.IsSuccessStatusCode)
                    continue;

                var xml = await response.Content.ReadAsStringAsync(ct);
                foreach (var loc in ParseSitemapLocs(xml, host))
                {
                    if (discovered.Count >= cap)
                        break;
                    if (!discovered.Contains(loc, StringComparer.OrdinalIgnoreCase))
                        discovered.Add(loc);
                }
            }
            catch
            {
                // Skip unreachable sitemaps; homepage audit still runs.
            }
        }

        return discovered;
    }

    private static async Task<IReadOnlyList<string>> ResolveSitemapUrlsAsync(Uri rootUri, CancellationToken ct)
    {
        var urls = new List<string>
        {
            $"{rootUri.Scheme}://{rootUri.Authority}/sitemap.xml",
            $"{rootUri.Scheme}://{rootUri.Authority}/sitemap_index.xml",
        };

        try
        {
            var robotsUrl = $"{rootUri.Scheme}://{rootUri.Authority}/robots.txt";
            using var response = await Http.GetAsync(robotsUrl, ct);
            if (!response.IsSuccessStatusCode)
                return urls;

            var text = await response.Content.ReadAsStringAsync(ct);
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                    continue;
                var sitemap = line["Sitemap:".Length..].Trim();
                if (sitemap.Length > 0 && !urls.Contains(sitemap, StringComparer.OrdinalIgnoreCase))
                    urls.Add(sitemap);
            }
        }
        catch
        {
            // robots.txt optional
        }

        return urls;
    }

    private static IReadOnlyList<string> ParseSitemapLocs(string xml, string host)
    {
        var urls = new List<string>();
        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            foreach (var loc in doc.Descendants(ns + "loc"))
            {
                var value = loc.Value?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    continue;
                if (!string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
                    continue;
                urls.Add(NormalizeUrl(uri));
            }
        }
        catch
        {
            // Invalid sitemap XML — caller may try another sitemap URL.
        }

        return urls;
    }

    private static string NormalizeUrl(Uri uri)
    {
        var path = uri.AbsolutePath;
        if (string.IsNullOrEmpty(path))
            path = "/";
        return $"{uri.Scheme}://{uri.Authority}{path}".TrimEnd('/');
    }
}
