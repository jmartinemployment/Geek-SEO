using System.Globalization;
using System.Net;
using GeekSeo.Application.Models.Seo;
using HtmlAgilityPack;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Fetch/render layer: mirrors search-engine protocol (render always, robots, status/redirects,
/// directives, URL identity, quiescence). Never deletes DOM nodes — annotates <c>data-gsv</c>
/// instead. Extraction decisions belong in <see cref="PageSectionTreeBuilder"/> and
/// <see cref="VisibleTextExtractor"/>. Uncapped same-origin BFS; robots.txt is the skip mechanism.
/// </summary>
public sealed class SitePageCrawler(ILogger<SitePageCrawler> logger)
{
    private const int CrawlConcurrency = 6;
    private const int InterRequestDelayMs = 100;
    private static readonly TimeSpan RobotsFetchTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Crawls the full same-origin inventory (homepage + sitemap/inventory seeds + BFS).
    /// Unlimited — no page cap. Requires a rendering browser; there is no HTTP fallback.
    /// </summary>
    public async Task<SiteCrawlData> CrawlAsync(
        string siteUrl,
        IReadOnlyList<string> sitemapUrls,
        IBrowser? browser,
        CancellationToken ct)
    {
        if (browser is null)
        {
            throw new InvalidOperationException(
                "crawl requires a rendering browser; Playwright/Chromium unavailable");
        }

        if (!TryGetOrigin(siteUrl, out var originUri, out var homepage))
            return new SiteCrawlData([], 0, 0);

        var queue = new Queue<string>();
        var queued = new HashSet<string>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pages = new List<CrawledPage>();
        var attempted = 0;

        var playwrightContext = await browser.NewContextAsync(CrawlerIdentity.MobileContext());

        await playwrightContext.RouteAsync("**/*", async route =>
        {
            if (TrackerBlocklist.ShouldAbort(route.Request.Url))
                await route.AbortAsync();
            else
                await route.ContinueAsync();
        });

        var robots = await FetchRobotsAsync(originUri, ct);
        var crawlDelayMs = robots.CrawlDelaySeconds is int seconds
            ? Math.Max(InterRequestDelayMs, seconds * 1000)
            : InterRequestDelayMs;

        try
        {
            Enqueue(homepage);
            foreach (var url in sitemapUrls)
            {
                if (TryNormalizeSameOrigin(url, originUri, out var normalized))
                    Enqueue(normalized);
            }

            foreach (var sitemap in robots.Sitemaps)
            {
                if (TryNormalizeSameOrigin(sitemap, originUri, out var normalized))
                    Enqueue(normalized);
            }

            var concurrency = CrawlConcurrency;
            var inFlight = 0;
            var distress = 0;
            var successStreak = 0;
            var gate = new object();

            async Task AcquireAsync()
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    lock (gate)
                    {
                        if (inFlight < concurrency)
                        {
                            inFlight++;
                            return;
                        }
                    }

                    await Task.Delay(25, ct);
                }
            }

            void Release()
            {
                lock (gate)
                    inFlight--;
            }

            void NoteDistress()
            {
                lock (gate)
                {
                    distress++;
                    successStreak = 0;
                    if (distress >= 3)
                        concurrency = Math.Max(1, concurrency / 2);
                }
            }

            void NoteSuccess()
            {
                lock (gate)
                {
                    successStreak++;
                    if (successStreak >= 8 && concurrency < CrawlConcurrency)
                        concurrency++;
                    if (distress > 0)
                        distress--;
                }
            }

            while (queue.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                var wave = new List<string>();
                while (queue.Count > 0)
                {
                    var url = queue.Dequeue();
                    if (seen.Add(url))
                        wave.Add(url);
                }

                if (wave.Count == 0)
                    break;

                attempted += wave.Count;
                var wavePages = new CrawledPage?[wave.Count];
                var fetchTasks = new Task[wave.Count];
                for (var i = 0; i < wave.Count; i++)
                {
                    var index = i;
                    var url = wave[i];
                    fetchTasks[i] = FetchWaveItemAsync(
                        AcquireAsync,
                        Release,
                        NoteDistress,
                        NoteSuccess,
                        playwrightContext,
                        url,
                        crawlDelayMs,
                        wavePages,
                        index,
                        ct);
                }

                await Task.WhenAll(fetchTasks);

                for (var i = 0; i < wave.Count; i++)
                {
                    var record = wavePages[i];
                    if (record is null)
                        continue;

                    pages.Add(record);

                    var finalKey = CrawlUrl.Canonicalize(record.FinalUrl.Length > 0 ? record.FinalUrl : record.Url);
                    seen.Add(finalKey);

                    if (record.StatusCode is < 200 or >= 300)
                        continue;

                    foreach (var link in record.Links)
                        Enqueue(link.Href);
                }
            }
        }
        finally
        {
            await playwrightContext.DisposeAsync();
        }

        logger.LogInformation(
            "Site crawl finished for {Origin}: {Fetched}/{Attempted} page(s) (unlimited, playwright-bfs)",
            originUri.GetLeftPart(UriPartial.Authority),
            pages.Count,
            attempted);

        return new SiteCrawlData(pages, attempted, pages.Count);

        void Enqueue(string url)
        {
            var key = CrawlUrl.Canonicalize(url);
            if (!seen.Contains(key) && queued.Add(key))
                queue.Enqueue(key);
        }
    }

    private async Task FetchWaveItemAsync(
        Func<Task> acquire,
        Action release,
        Action noteDistress,
        Action noteSuccess,
        IBrowserContext playwrightContext,
        string url,
        int crawlDelayMs,
        CrawledPage?[] wavePages,
        int index,
        CancellationToken ct)
    {
        await acquire();
        try
        {
            ct.ThrowIfCancellationRequested();
            if (crawlDelayMs > 0)
                await Task.Delay(crawlDelayMs, ct);
            var record = await FetchWithPlaywrightAsync(playwrightContext, url, ct);
            wavePages[index] = record;
            if (record is not null && record.StatusCode is 429 or >= 500)
                noteDistress();
            else if (record is { StatusCode: >= 200 and < 400 })
                noteSuccess();
        }
        finally
        {
            release();
        }
    }

    internal static IEnumerable<string> ExtractSameOriginLinks(string html, string pageUrl, string origin)
    {
        foreach (var link in ExtractLinksFromHtml(html, pageUrl, origin))
            yield return link.Href;
    }

    internal static IReadOnlyList<DiscoveredCrawlLink> ExtractLinksFromHtml(
        string html, string pageUrl, string origin)
    {
        if (string.IsNullOrWhiteSpace(html))
            return [];

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var nodes = doc.DocumentNode.SelectNodes(".//a[@href]");
        if (nodes is null)
            return [];

        var links = new List<DiscoveredCrawlLink>();
        foreach (var anchor in nodes)
        {
            var href = WebUtility.HtmlDecode(anchor.GetAttributeValue("href", "")).Trim();
            if (string.IsNullOrWhiteSpace(href))
                continue;
            if (!TryResolveUrl(href, pageUrl, origin, out var absolute))
                continue;

            var text = VisibleTextExtractor.Collapse(
                WebUtility.HtmlDecode(anchor.InnerText ?? string.Empty));
            var rel = (anchor.GetAttributeValue("rel", "") ?? string.Empty).Trim().ToLowerInvariant();
            links.Add(new DiscoveredCrawlLink(absolute, text, rel));
        }

        return links;
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

            if (!Uri.TryCreate(origin.Contains("://", StringComparison.Ordinal) ? origin : "https://" + origin,
                    UriKind.Absolute, out var originUri))
                return false;

            if (!CrawlUrl.IsSameSite(resolved, originUri))
                return false;

            absolute = CrawlUrl.Canonicalize(resolved.AbsoluteUri);
            return true;
        }
        catch
        {
            return false;
        }
    }


    private async Task<CrawledPage?> FetchWithPlaywrightAsync(
        IBrowserContext context, string url, CancellationToken ct)
    {
        IPage? page = null;
        try
        {
            page = await context.NewPageAsync();
            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = CrawlerIdentity.NavigationTimeoutMs,
            });

            if (response is null)
            {
                return new CrawledPage(url, string.Empty, "playwright")
                {
                    FinalUrl = url,
                    StatusCode = 0,
                    FetchedAt = DateTimeOffset.UtcNow,
                };
            }

            await CrawlerIdentity.WaitForRenderedAsync(page);

            var redirectChain = CaptureRedirectChain(response);
            var finalUrl = response.Url ?? page.Url ?? url;
            var headers = response.Headers;
            headers.TryGetValue("x-robots-tag", out var xRobots);
            headers.TryGetValue("content-type", out var contentType);
            var status = response.Status;

            var html = await AnnotateVisibilityAsync(page);
            var directives = ParseDirectives(html, xRobots);
            var links = status is >= 200 and < 300
                ? ExtractLinksFromHtml(html, finalUrl, url)
                : [];

            return new CrawledPage(url, html ?? string.Empty, "playwright")
            {
                FinalUrl = finalUrl,
                StatusCode = status,
                RedirectChain = redirectChain,
                Canonical = directives.Canonical,
                NoIndex = directives.NoIndex,
                NoFollow = directives.NoFollow,
                SoftNotFound = directives.SoftNotFound,
                FetchedAt = DateTimeOffset.UtcNow,
                Links = links,
                ContentType = contentType,
            };
        }
        finally
        {
            if (page is not null)
                await page.CloseAsync();
        }
    }

    private static async Task<string> AnnotateVisibilityAsync(IPage page)
    {
        var hiddenScript =
            """
            () => [...document.querySelectorAll('*')].map(el => {
              const s = getComputedStyle(el);
              return s.display === 'none' || s.visibility === 'hidden';
            })
            """;

        var atMobile = await page.EvaluateAsync<bool[]>(hiddenScript);
        await page.SetViewportSizeAsync(CrawlerIdentity.DesktopProbeWidth, CrawlerIdentity.DesktopProbeHeight);
        try
        {
            await page.EvaluateAsync("() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)))");
            var atDesktop = await page.EvaluateAsync<bool[]>(hiddenScript);
            var labels = VisibilityClassifier.ClassifyAll(atMobile, atDesktop);
            await page.EvaluateAsync(
                """
                labels => {
                  const all = [...document.querySelectorAll('*')];
                  const n = Math.min(all.length, labels.length);
                  for (let i = 0; i < n; i++) all[i].setAttribute('data-gsv', labels[i]);
                }
                """,
                labels);
        }
        finally
        {
            await page.SetViewportSizeAsync(
                CrawlerIdentity.MobileViewportWidth,
                CrawlerIdentity.MobileViewportHeight);
        }

        return await page.EvaluateAsync<string>("() => document.documentElement.outerHTML") ?? string.Empty;
    }

    private static List<string> CaptureRedirectChain(IResponse response)
    {
        var chain = new List<string>();
        var from = response.Request.RedirectedFrom;
        while (from is not null)
        {
            chain.Insert(0, from.Url);
            from = from.RedirectedFrom;
        }

        return chain;
    }


    private readonly record struct PageDirectives(string? Canonical, bool NoIndex, bool NoFollow, bool SoftNotFound);

    private static PageDirectives ParseDirectives(string html, string? xRobotsTag)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html ?? string.Empty);

        var tokens = new List<string>();
        foreach (var meta in doc.DocumentNode.SelectNodes("//meta[@name]") ?? Enumerable.Empty<HtmlNode>())
        {
            var name = meta.GetAttributeValue("name", "");
            if (!name.Equals("robots", StringComparison.OrdinalIgnoreCase)
                && !name.Equals("googlebot", StringComparison.OrdinalIgnoreCase))
                continue;
            tokens.AddRange(SplitDirectiveTokens(meta.GetAttributeValue("content", "")));
        }

        if (!string.IsNullOrWhiteSpace(xRobotsTag))
            tokens.AddRange(SplitDirectiveTokens(xRobotsTag));

        var noindex = tokens.Any(t => t is "noindex" or "none");
        var nofollow = tokens.Any(t => t is "nofollow" or "none");

        string? canonical = null;
        var link = doc.DocumentNode.SelectSingleNode("//link[translate(@rel,'CANONICAL','canonical')='canonical']")
                   ?? doc.DocumentNode.SelectSingleNode("//link[@rel='canonical' or @rel='Canonical']");
        if (link is not null)
        {
            var href = link.GetAttributeValue("href", "").Trim();
            canonical = string.IsNullOrWhiteSpace(href) ? null : href;
        }

        var title = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "");
        var h1 = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//h1")?.InnerText ?? "");
        var h2 = WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//h2")?.InnerText ?? "");
        var soft = IsSoftNotFoundText(title) || IsSoftNotFoundText(h1) || IsSoftNotFoundText(h2);

        return new PageDirectives(canonical, noindex, nofollow, soft);
    }

    private static IEnumerable<string> SplitDirectiveTokens(string content) =>
        content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant());

    private static bool IsSoftNotFoundText(string text)
    {
        var value = VisibleTextExtractor.Collapse(text).ToLowerInvariant();
        if (value.Length == 0)
            return false;
        return value.StartsWith("404", StringComparison.Ordinal)
            || value.Contains("not found", StringComparison.Ordinal)
            || value.Contains("page not found", StringComparison.Ordinal);
    }

    private static bool TryNormalizeSameOrigin(string url, Uri originUri, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        if (!CrawlUrl.IsSameSite(uri, originUri))
            return false;
        normalized = CrawlUrl.Canonicalize(uri.AbsoluteUri);
        return true;
    }

    private static bool TryGetOrigin(string siteUrl, out Uri originUri, out string homepage)
    {
        originUri = null!;
        homepage = string.Empty;
        try
        {
            originUri = new Uri(SiteUrlNormalizer.Normalize(siteUrl));
            homepage = CrawlUrl.Canonicalize(originUri.GetLeftPart(UriPartial.Authority) + "/");
            return true;
        }
        catch
        {
            return false;
        }
    }


    private async Task<RobotsRules> FetchRobotsAsync(Uri originUri, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = RobotsFetchTimeout };
        var rules = await RobotsTxt.FetchAsync(originUri, client, ct);
        logger.LogInformation(
            "robots.txt for {Origin}: sitemaps={SitemapCount}, crawl-delay={CrawlDelay}",
            originUri.GetLeftPart(UriPartial.Authority),
            rules.Sitemaps.Count,
            rules.CrawlDelaySeconds);
        return rules;
    }
}
