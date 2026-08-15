using System.Text.Json;
using System.Text.RegularExpressions;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Services.SiteExtraction;
using Microsoft.Playwright;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>
/// Single-page crawler used by scoring. Shares the RFC 9309 / Google REP parser with
/// <see cref="SitePageCrawler"/> via <see cref="RobotsTxt"/>. Unreachable or 5xx robots.txt
/// fails closed (disallow all).
/// </summary>
public sealed class PlaywrightCrawlerProvider(IBrowser browser) : ICrawlerProvider
{
    private static readonly SemaphoreSlim CrawlSemaphore = new(2, 2);
    private static readonly HttpClient RobotsClient = new() { Timeout = TimeSpan.FromSeconds(8) };

    public string ProviderName => "playwright";

    public async Task<Result<PageContent>> CrawlPageAsync(string url, CancellationToken ct = default)
    {
        if (!await IsAllowedByRobotsTxtAsync(url, ct))
            return Result<PageContent>.Failure($"URL {url} disallowed by robots.txt");

        await CrawlSemaphore.WaitAsync(ct);
        try
        {
            await using var context = await browser.NewContextAsync(CrawlerIdentity.MobileContext());
            var page = await context.NewPageAsync();

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                Timeout = CrawlerIdentity.NavigationTimeoutMs,
                WaitUntil = WaitUntilState.Load,
            });
            await CrawlerIdentity.WaitForRenderedAsync(page);

            var httpStatus = response?.Status ?? 0;
            if (httpStatus >= 400)
                return Result<PageContent>.Failure($"HTTP {httpStatus} for {url}");

            var metaTitle = await page.TitleAsync();
            var bodyText = await page.EvalOnSelectorAsync<string>("body", "el => el ? el.innerText : ''") ?? string.Empty;
            var metaDescription = await page.GetAttributeAsync("meta[name=\"description\"]", "content");
            var headings = await ExtractHeadingsAsync(page);
            var structuredTypes = await ExtractStructuredDataTypesAsync(page);
            var wordCount = CountWords(bodyText);

            return Result<PageContent>.Success(new PageContent
            {
                Url = url,
                FullText = bodyText,
                MetaTitle = metaTitle,
                MetaDescription = metaDescription,
                WordCount = wordCount,
                HttpStatusCode = httpStatus,
                Headings = headings,
                HasStructuredData = structuredTypes.Count > 0,
                StructuredDataTypes = structuredTypes,
                CrawledAt = DateTimeOffset.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return Result<PageContent>.Failure($"Crawl failed for {url}: {ex.Message}");
        }
        finally
        {
            CrawlSemaphore.Release();
        }
    }

    public async Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var rules = await RobotsTxt.FetchAsync(uri, RobotsClient, ct);
        return rules.IsAllowed(uri.AbsolutePath);
    }

    private static async Task<IReadOnlyList<PageHeading>> ExtractHeadingsAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(@"() => {
            return JSON.stringify(
                Array.from(document.querySelectorAll('h1,h2,h3,h4,h5,h6'))
                    .map(el => ({ level: Number(el.tagName.substring(1)), text: el.innerText.trim() }))
                    .filter(h => h.text.length > 0)
            );
        }");

        return JsonSerializer.Deserialize<List<PageHeading>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static async Task<IReadOnlyList<string>> ExtractStructuredDataTypesAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(@"() => {
            const types = new Set();
            document.querySelectorAll('script[type=""application/ld+json""]').forEach(node => {
                try {
                    const data = JSON.parse(node.textContent || '{}');
                    const collect = (obj) => {
                        if (!obj || typeof obj !== 'object') return;
                        if (obj['@type']) types.add(String(obj['@type']));
                        Object.values(obj).forEach(collect);
                    };
                    collect(data);
                } catch {}
            });
            return JSON.stringify(Array.from(types));
        }");

        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text)
            ? 0
            : Regex.Split(text.Trim(), @"\s+").Length;
}
