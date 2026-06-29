using Microsoft.Playwright;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp;

public sealed class GooglePlaywrightFetcher : IAsyncDisposable
{
    private const string SerpSelector = "#search, #rso, div.g, [data-sokoban-container]";

    private static readonly string[] ConsentSelectors =
    [
        "#L2AGLb",
        "button:has-text(\"Accept all\")",
        "button:has-text(\"I agree\")",
        "button:has-text(\"Reject all\")",
        "[aria-label=\"Accept all\"]",
        "#W0wltc"
    ];

    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task<string> FetchHtmlAsync(string url, string userAgent, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var headless = ResolveHeadless();
            var channel = ResolveBrowserChannel();

            _playwright ??= await Playwright.CreateAsync();
            _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless,
                Channel = channel,
                Args =
                [
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-blink-features=AutomationControlled"
                ]
            });

            var page = await _browser.NewPageAsync(new BrowserNewPageOptions
            {
                UserAgent = userAgent,
                ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
                Locale = "en-US",
                TimezoneId = "America/New_York"
            });

            try
            {
                await page.AddInitScriptAsync(
                    "Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 45_000
                });

                await TryDismissGoogleConsentAsync(page);

                try
                {
                    await page.WaitForSelectorAsync(
                        SerpSelector,
                        new PageWaitForSelectorOptions { Timeout = 30_000 });
                }
                catch (TimeoutException)
                {
                    var diagnostic = await BuildTimeoutDiagnosticAsync(page);
                    throw new SerpFetchException(
                        $"SERP Playwright fetch failed ({diagnostic}). Stage failed.");
                }

                return await page.ContentAsync();
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task TryDismissGoogleConsentAsync(IPage page)
    {
        foreach (var selector in ConsentSelectors)
        {
            try
            {
                var button = page.Locator(selector).First;
                try
                {
                    await button.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 1500
                    });
                }
                catch (TimeoutException)
                {
                    continue;
                }

                await button.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions
                {
                    Timeout = 10_000
                });
                return;
            }
            catch (PlaywrightException)
            {
                // Try the next consent selector.
            }
        }
    }

    private static async Task<string> BuildTimeoutDiagnosticAsync(IPage page)
    {
        var currentUrl = page.Url;
        var html = await page.ContentAsync();

        if (GoogleSerpHtmlParser.LooksLikeBlockedOrCaptcha(html))
            return "CAPTCHA or block page after Playwright load";

        if (currentUrl.Contains("consent.google", StringComparison.OrdinalIgnoreCase)
            || html.Contains("Before you continue", StringComparison.OrdinalIgnoreCase)
            || html.Contains("consent.google.com", StringComparison.OrdinalIgnoreCase))
        {
            return "Google consent page — could not dismiss cookie dialog";
        }

        if (GoogleSerpHtmlParser.LooksLikeJavaScriptRequired(html))
            return "JavaScript-only Google shell after Playwright load";

        return $"Timeout waiting for SERP layout at {currentUrl}";
    }

    private static bool ResolveHeadless()
    {
        var value = Environment.GetEnvironmentVariable("GOOGLE_SCRAPE_HEADLESS")?.Trim();
        return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveBrowserChannel()
    {
        var channel = Environment.GetEnvironmentVariable("GOOGLE_SCRAPE_BROWSER_CHANNEL")?.Trim();
        return string.IsNullOrWhiteSpace(channel) ? null : channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
        _gate.Dispose();
    }
}
