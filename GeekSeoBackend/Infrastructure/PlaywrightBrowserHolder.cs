using GeekSeoBackend.Services.SiteExtraction;
using Microsoft.Playwright;

namespace GeekSeoBackend.Infrastructure;

public sealed class PlaywrightBrowserHolder : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;

    public IBrowser? Browser { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await LaunchCoreAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"ERROR: Playwright/Chromium launch failed. Crawls fail closed until retry. {ex}");
            Browser = null;
        }
    }

    /// <summary>Retries a failed startup launch so a transient Chromium failure does not poison the process.</summary>
    public async Task<IBrowser?> EnsureBrowserAsync(CancellationToken ct = default)
    {
        if (Browser is not null)
            return Browser;

        await _gate.WaitAsync(ct);
        try
        {
            if (Browser is not null)
                return Browser;

            try
            {
                await LaunchCoreAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"ERROR: Playwright/Chromium retry launch failed. {ex}");
                Browser = null;
            }

            return Browser;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task LaunchCoreAsync()
    {
        _playwright ??= await Playwright.CreateAsync();
        CrawlerIdentity.UsePlaywrightDevices(_playwright);
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
