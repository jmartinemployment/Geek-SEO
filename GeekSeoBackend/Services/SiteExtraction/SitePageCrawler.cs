using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Fetches a bounded set of same-origin pages (homepage, sitemap seeds, shallow BFS)
/// for internal-link and URL-pattern extractors.
/// HTTP-only runs (manual step re-run) crawl seeds only — BFS link following requires Playwright
/// because SPA shells return the same HTML for every client-side route.
/// </summary>
public sealed partial class SitePageCrawler(
    IHttpClientFactory factory,
    ILogger<SitePageCrawler> logger)
{
    // Matches Playwright's page.GotoAsync timeout (15_000 ms) so HTTP-only crawl mode
    // doesn't time out URLs sooner than the Playwright-backed BFS mode would.
    private const int HttpFetchTimeoutSeconds = 15;

    private static readonly string[] SkipExtensions =
    [
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".zip", ".xml",
        ".css", ".js", ".woff", ".woff2",
    ];

    /// <summary>
    /// Crawls the full same-origin inventory (homepage + sitemap/inventory seeds + BFS-discovered
    /// links when Playwright is available). Unlimited — no page cap, no attempt-budget soft-stop.
    /// Callers that need a bounded crawl must apply their own post-hoc limiting; this method always
    /// attempts every reachable, non-junk URL so crawl completeness can be checked against inventory.
    /// </summary>
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
        foreach (var url in sitemapUrls)
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
        var followLinks = playwrightContext is not null;

        try
        {
            while (queue.Count > 0)
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

                pages.Add(new CrawledPage(url, html, playwrightContext is not null ? "playwright" : "http"));

                if (followLinks)
                {
                    foreach (var discovered in ExtractSameOriginLinks(html, url, origin))
                        Enqueue(discovered);
                }
            }
        }
        finally
        {
            if (playwrightContext is not null)
                await playwrightContext.DisposeAsync();
        }

        logger.LogInformation(
            "Site crawl finished for {Origin}: {Fetched}/{Attempted} page(s) (unlimited, mode {Mode})",
            origin,
            pages.Count,
            attempted,
            followLinks ? "playwright-bfs" : "http-seeds-only");

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

    /// <summary>
    /// Hard-junk-only crawl filter. Utility pages (/about, /contact, /faq, …) are legitimate
    /// sitemap-inventory / site-audit URLs and MUST be fetched — they are excluded only from
    /// pillar/topic selection via <see cref="NoisePaths"/>, never from the crawl itself.
    /// </summary>
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
        return segments.Any(HardJunkPaths.IsHardJunk);
    }

    private async Task<string?> FetchWithPlaywrightAsync(
        IBrowserContext context, string url, CancellationToken ct)
    {
        IPage? page = null;
        try
        {
            page = await context.NewPageAsync();
            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 15_000,
            });
            if (response is null || !response.Ok)
            {
                logger.LogDebug("Playwright crawl skipped {Url}: HTTP {Status}", url, response?.Status ?? -1);
                return null;
            }
            await page.WaitForTimeoutAsync(1000);
            var html = await page.ContentAsync();

            if (IsSoft404(html, url))
            {
                logger.LogDebug("Playwright crawl skipped {Url}: soft 404 detected", url);
                return null;
            }

            return html;
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

    private static bool IsSoft404(string html, string url)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        html = html.ToLowerInvariant();

        if (html.Contains("<meta name=\"robots\" content=\"noindex\"") ||
            html.Contains("<meta name='robots' content='noindex'"))
            return true;

        if (html.Contains("<title>404") || html.Contains("<title>not found") ||
            html.Contains("<h1>404") || html.Contains("<h1>not found") ||
            html.Contains("<h1>page not found") || html.Contains("<h2>404") ||
            html.Contains("<h2>not found"))
            return true;

        return false;
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
            var uri = new Uri(SiteUrlNormalizer.Normalize(siteUrl));
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
        client.Timeout = TimeSpan.FromSeconds(HttpFetchTimeoutSeconds);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)");
        return client;
    }

    [GeneratedRegex(
        @"<a\s[^>]*href=[""']([^""'#][^""']*)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinkHrefRegex();
}
