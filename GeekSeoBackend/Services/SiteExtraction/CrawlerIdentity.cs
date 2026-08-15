using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Bot identity plus Playwright's <c>devices['Pixel 7']</c> profile (viewport, DPR, touch,
/// mobile UA). That is device emulation, not a separate rendering engine — Chromium is the
/// engine; Pixel 7 is the device descriptor.
/// </summary>
internal static class CrawlerIdentity
{
    public const string PlaywrightDeviceName = "Pixel 7";

    /// <summary>Playwright 1.51 <c>devices['Pixel 7']</c> UA plus our product token.</summary>
    public const string UserAgent =
        "Mozilla/5.0 (Linux; Android 14; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/134.0.6998.35 Mobile Safari/537.36 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)";

    public static int MobileViewportWidth { get; private set; } = 412;
    public static int MobileViewportHeight { get; private set; } = 839;
    public static float DeviceScaleFactor { get; private set; } = 2.625f;
    public static int MobileScreenWidth { get; private set; } = 412;
    public static int MobileScreenHeight { get; private set; } = 915;

    public const int DesktopProbeWidth = 1280;
    public const int DesktopProbeHeight = 900;

    public const int RenderQuiescenceCapMs = 5000;
    public const int NavigationTimeoutMs = 30_000;

    private static BrowserNewContextOptions? _playwrightPixel7;

    /// <summary>
    /// Bind the installed Playwright device registry so we use the same Pixel 7 profile as
    /// <c>...devices['Pixel 7']</c> in Node, not a hand-typed approximation.
    /// </summary>
    public static void UsePlaywrightDevices(IPlaywright playwright)
    {
        if (!playwright.Devices.TryGetValue(PlaywrightDeviceName, out var device))
        {
            throw new InvalidOperationException(
                $"Playwright.Devices has no '{PlaywrightDeviceName}'");
        }

        _playwrightPixel7 = ClonePixel7(device);
        if (device.ViewportSize is { } vp)
        {
            MobileViewportWidth = vp.Width;
            MobileViewportHeight = vp.Height;
        }

        if (device.ScreenSize is { } screen)
        {
            MobileScreenWidth = screen.Width;
            MobileScreenHeight = screen.Height;
        }

        if (device.DeviceScaleFactor is { } dpr)
            DeviceScaleFactor = (float)dpr;
    }

    /// <summary>
    /// Chromium context for every fetch: Playwright Pixel 7 device + GeekSEO token + mobile hints.
    /// </summary>
    public static BrowserNewContextOptions MobileContext() =>
        ClonePixel7(_playwrightPixel7) ?? FallbackPixel7();

    public static async Task WaitForRenderedAsync(IPage page)
    {
        try
        {
            await page.WaitForLoadStateAsync(
                LoadState.NetworkIdle,
                new PageWaitForLoadStateOptions { Timeout = RenderQuiescenceCapMs });
        }
        catch (PlaywrightException)
        {
            // Cap reached — snapshot anyway. Never shorten fidelity to buy latency.
        }
    }

    private static BrowserNewContextOptions FallbackPixel7() => new()
    {
        UserAgent = UserAgent,
        ViewportSize = new ViewportSize
        {
            Width = MobileViewportWidth,
            Height = MobileViewportHeight,
        },
        ScreenSize = new ScreenSize
        {
            Width = MobileScreenWidth,
            Height = MobileScreenHeight,
        },
        IsMobile = true,
        HasTouch = true,
        DeviceScaleFactor = DeviceScaleFactor,
        Locale = "en-US",
        ExtraHTTPHeaders = MobileHeaders(),
    };

    private static BrowserNewContextOptions ClonePixel7(BrowserNewContextOptions? source)
    {
        if (source is null)
            return FallbackPixel7();

        return new BrowserNewContextOptions
        {
            UserAgent = WithBotToken(source.UserAgent),
            ViewportSize = source.ViewportSize is { } vp
                ? new ViewportSize { Width = vp.Width, Height = vp.Height }
                : new ViewportSize { Width = MobileViewportWidth, Height = MobileViewportHeight },
            ScreenSize = source.ScreenSize is { } screen
                ? new ScreenSize { Width = screen.Width, Height = screen.Height }
                : new ScreenSize { Width = MobileScreenWidth, Height = MobileScreenHeight },
            IsMobile = source.IsMobile ?? true,
            HasTouch = source.HasTouch ?? true,
            DeviceScaleFactor = source.DeviceScaleFactor ?? DeviceScaleFactor,
            Locale = "en-US",
            ExtraHTTPHeaders = MobileHeaders(),
        };
    }

    private static string WithBotToken(string? deviceUserAgent)
    {
        if (string.IsNullOrWhiteSpace(deviceUserAgent))
            return UserAgent;
        if (deviceUserAgent.Contains("GeekSEO", StringComparison.Ordinal))
            return deviceUserAgent;
        return $"{deviceUserAgent} (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)";
    }

    private static Dictionary<string, string> MobileHeaders() => new()
    {
        ["Sec-Ch-Ua-Mobile"] = "?1",
        ["Accept-Language"] = "en-US,en;q=0.9",
    };
}
