using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Fetches a bounded set of same-origin pages (homepage, sitemap seeds, shallow BFS)
/// for internal-link and URL-pattern extractors.
/// </summary>
public sealed partial class SitePageCrawler(
    IHttpClientFactory factory,
    ILogger<SitePageCrawler> logger)
{
    private const int MaxPages = 20;
    private const int MaxSitemapSeeds = 10;
    private static readonly string[] SkipExtensions =
    [
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".zip", ".xml",
        ".css", ".js", ".woff", ".woff2",
    ];

    public async Task<SiteCrawlData> CrawlAsync(
        string siteUrl,
        IReadOnlyList<string> sitemapUrls,
        IBrowser? browser,
        CancellationToken ct)
    {
        if (!TryGetOrigin(siteUrl, out var origin, out var homepage))
            return new SiteCrawlData([], 0, 0);

        var queue = new Queue<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pages = new List<CrawledPage>();
        var attempted = 0;

        Enqueue(homepage);
        foreach (var url in sitemapUrls.Take(MaxSitemapSeeds))
        {
            if (TryNormalizeSameOrigin(url, origin, out var normalized))
                Enqueue(normalized);
        }

        IBrowserContext? playwrightContext = null;
        if (browser is not null)
        {
            playwrightContext = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)",
            });
        }

        var client = playwrightContext is null ? BuildClient() : null;

        try
        {
            while (queue.Count > 0 && pages.Count < MaxPages)
            {
                ct.ThrowIfCancellationRequested();
                var url = queue.Dequeue();
                if (!seen.Add(url))
                    continue;

                attempted++;
                string? html = playwrightContext is not null
                    ? await FetchWithPlaywrightAsync(playwrightContext, url, ct)
                    : await FetchWithHttpAsync(client!, url, ct);

                if (string.IsNullOrWhiteSpace(html))
                    continue;

                pages.Add(new CrawledPage(url, html));

                foreach (var discovered in ExtractSameOriginLinks(html, url, origin))
                {
                    if (pages.Count + queue.Count >= MaxPages * 2)
                        break;
                    Enqueue(discovered);
                }
            }
        }
        finally
        {
            if (playwrightContext is not null)
                await playwrightContext.DisposeAsync();
        }

        return new SiteCrawlData(pages, attempted, pages.Count);

        void Enqueue(string url)
        {
            if (ShouldSkipUrl(url))
                return;
            if (!seen.Contains(url) && !queue.Contains(url))
                queue.Enqueue(url);
        }
    }

    internal static IEnumerable<string> ExtractSameOriginLinks(string html, string pageUrl, string origin)
    {
        foreach (Match match in LinkHrefRegex().Matches(html))
        {
            var href = WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
            if (string.IsNullOrWhiteSpace(href))
                continue;

            if (!TryResolveUrl(href, pageUrl, origin, out var absolute))
                continue;

            if (ShouldSkipUrl(absolute))
                continue;

            yield return absolute;
        }
    }

    internal static bool TryResolveUrl(string href, string pageUrl, string origin, out string absolute)
    {
        absolute = string.Empty;

        if (href.StartsWith('#') ||
            href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var baseUri = new Uri(pageUrl);
            if (!Uri.TryCreate(baseUri, href, out var resolved))
                return false;

            if (!resolved.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return false;

            var resolvedOrigin = resolved.GetLeftPart(UriPartial.Authority);
            if (!resolvedOrigin.Equals(origin, StringComparison.OrdinalIgnoreCase))
                return false;

            absolute = StripFragment(resolved.AbsoluteUri);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ShouldSkipUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        var path = url;
        try
        {
            path = new Uri(url).AbsolutePath;
        }
        catch
        {
            return true;
        }

        foreach (var ext in SkipExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return false;

        // Skip utility pages (/contact, /about) but still crawl /services/accounting.
        if (segments.Length == 1)
            return NoisePaths.IsNoise(segments[0]);

        return segments.All(NoisePaths.IsNoise);
    }

    private async Task<string?> FetchWithPlaywrightAsync(
        IBrowserContext context, string url, CancellationToken ct)
    {
        IPage? page = null;
        try
        {
            page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 15_000,
            });
            await page.WaitForTimeoutAsync(400);
            return await page.ContentAsync();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Playwright crawl skipped {Url}", url);
            return null;
        }
        finally
        {
            if (page is not null)
                await page.CloseAsync();
        }
    }

    private async Task<string?> FetchWithHttpAsync(HttpClient client, string url, CancellationToken ct)
    {
        try
        {
            return await client.GetStringAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "HTTP crawl skipped {Url}", url);
            return null;
        }
    }

    private static bool TryNormalizeSameOrigin(string url, string origin, out string normalized)
    {
        normalized = string.Empty;
        if (!url.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
            return false;

        normalized = StripFragment(url);
        return !ShouldSkipUrl(normalized);
    }

    private static bool TryGetOrigin(string siteUrl, out string origin, out string homepage)
    {
        origin = string.Empty;
        homepage = string.Empty;
        try
        {
            var uri = new Uri(NicheSiteUrlNormalizer.Normalize(siteUrl));
            origin = uri.GetLeftPart(UriPartial.Authority);
            homepage = origin + "/";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string StripFragment(string url)
    {
        var hash = url.IndexOf('#');
        return hash >= 0 ? url[..hash] : url;
    }

    private HttpClient BuildClient()
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)");
        return client;
    }

    [GeneratedRegex(
        @"<a\s[^>]*href=[""']([^""'#][^""']*)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinkHrefRegex();
}
