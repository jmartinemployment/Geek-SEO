using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheExtraction;

public sealed class NavMenuExtractor(ILogger<NavMenuExtractor> logger)
{
    private const int NavTimeoutMs = 45_000;

    private static readonly string[] NavSelectors =
    [
        "nav a",
        "header nav a",
        "[role='navigation'] a",
        ".nav a",
        ".navbar a",
        ".menu a",
        "#menu a",
        ".navigation a",
        ".site-nav a",
        "header a",
    ];

    public async Task<NavMenuData> ExtractAsync(string siteUrl, IBrowser browser, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(NavTimeoutMs);

        IPage? page = null;
        try
        {
            page = await browser.NewPageAsync();
            await page.SetViewportSizeAsync(1440, 900);
            await page.GotoAsync(
                siteUrl,
                new PageGotoOptions
                {
                    Timeout = 15_000,
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                });

            await page.WaitForTimeoutAsync(800);

            // Extract only links visible without interaction — search engines don't
            // click hamburger menus or expand hidden navigation.
            var links = await ExtractLinksAsync(page, siteUrl);

            timeoutCts.Token.ThrowIfCancellationRequested();

            var pillars = BuildPillars(links, siteUrl);
            return new NavMenuData(pillars, links.Count < 2 ? "fallback" : "nav");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            logger.LogWarning("Nav menu extraction timed out after {Ms}ms for {Url}", NavTimeoutMs, siteUrl);
            return new NavMenuData([], "timeout");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nav menu extraction failed for {Url}", siteUrl);
            return new NavMenuData([], "failed");
        }
        finally
        {
            if (page is not null)
            {
                try
                {
                    await page.CloseAsync();
                }
                catch
                {
                    // Best-effort cleanup after timeout/cancel
                }
            }
        }
    }

    private static async Task<List<(string Text, string Href)>> ExtractLinksAsync(IPage page, string siteUrl)
    {
        var origin = GetOrigin(siteUrl);

        foreach (var selector in NavSelectors)
        {
            try
            {
                var links = await page.EvaluateAsync<List<(string text, string href)>>($@"
                    Array.from(document.querySelectorAll('{selector}'))
                        .map(a => ({{ text: a.textContent?.trim() ?? '', href: a.href ?? '' }}))
                        .filter(l => l.text && l.href)
                ");

                var filtered = links
                    .Where(l => !string.IsNullOrWhiteSpace(l.text) && !string.IsNullOrWhiteSpace(l.href))
                    .Where(l => l.href.StartsWith(origin, StringComparison.OrdinalIgnoreCase) || l.href.StartsWith("/"))
                    .Where(l => !NoisePaths.IsNoise(l.text.ToLowerInvariant().Replace(" ", "-")))
                    .Distinct()
                    .ToList();

                if (filtered.Count >= 2) return filtered;
            }
            catch
            {
                // Try next selector
            }
        }

        return [];
    }

    private static List<DiscoveredPillar> BuildPillars(List<(string Text, string Href)> links, string siteUrl)
    {
        var origin = GetOrigin(siteUrl);
        var pillars = new Dictionary<string, DiscoveredPillar>(StringComparer.OrdinalIgnoreCase);

        foreach (var (text, href) in links)
        {
            var slug = TextToSlug(text);
            if (NoisePaths.IsNoise(slug)) continue;

            var relativePath = href.StartsWith(origin, StringComparison.OrdinalIgnoreCase)
                ? href[origin.Length..]
                : href;
            relativePath = relativePath.TrimStart('/');

            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0) continue;

            if (segments.Length >= 2 && !NoisePaths.IsNoise(segments[1]))
            {
                var childSlug = segments[1];
                if (!pillars.ContainsKey(childSlug))
                {
                    pillars[childSlug] = new DiscoveredPillar
                    {
                        Name = text,
                        Slug = childSlug,
                        PageUrl = "/" + relativePath,
                        Intent = SitemapExtractor.InferIntent(segments[0]),
                        Source = "nav",
                        ChildPageCount = 1,
                    };
                }
                continue;
            }

            if (!pillars.ContainsKey(slug))
            {
                pillars[slug] = new DiscoveredPillar
                {
                    Name = text,
                    Slug = slug,
                    PageUrl = "/" + relativePath,
                    Intent = SitemapExtractor.InferIntent(slug),
                    Source = "nav",
                    ChildPageCount = 0,
                };
            }
        }

        return pillars.Values.ToList();
    }

    private static string GetOrigin(string siteUrl)
    {
        try { return new Uri(siteUrl).GetLeftPart(UriPartial.Authority); }
        catch { return siteUrl.TrimEnd('/'); }
    }

    private static string TextToSlug(string text) =>
        System.Text.RegularExpressions.Regex.Replace(
            text.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-")
            .Trim('-');
}
