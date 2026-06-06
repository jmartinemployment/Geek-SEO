using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Extracts service-like phrases from visible homepage content (lists, section headings).
/// H3 section titles are tracked separately as page verticals (e.g. Accounting).
/// </summary>
public sealed partial class PageContentExtractor(
    IHttpClientFactory factory,
    ILogger<PageContentExtractor> logger)
{
    private const int MaxPhrases = 40;
    private const int MaxVerticalTopics = 12;

    public async Task<PageContentData> ExtractAsync(
        string siteUrl, IBrowser? browser, CancellationToken ct)
    {
        IReadOnlyList<string> phrases;
        IReadOnlyList<string> verticalTopics;
        int listItemsScanned;

        if (browser is not null)
        {
            try
            {
                (phrases, verticalTopics, listItemsScanned) =
                    await ExtractWithPlaywrightAsync(siteUrl, browser, ct);
                return new PageContentData(phrases, verticalTopics, listItemsScanned);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Playwright page content extraction failed for {Url}", siteUrl);
            }
        }

        (phrases, verticalTopics, listItemsScanned) = await ExtractFromHttpAsync(siteUrl, ct);
        return new PageContentData(phrases, verticalTopics, listItemsScanned);
    }

    private async Task<(IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned)> ExtractWithPlaywrightAsync(
        string siteUrl, IBrowser browser, CancellationToken ct)
    {
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)",
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(siteUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 20_000,
        });

        var payload = await page.EvaluateAsync<string>(@"() => {
            const phrases = [];
            const verticalTopics = [];
            const seen = new Set();
            const seenVertical = new Set();

            const add = (text, target) => {
                const t = (text || '').replace(/\s+/g, ' ').trim();
                if (t.length < 4 || t.length > 80) return;
                const key = t.toLowerCase();
                const bucket = target === 'vertical' ? seenVertical : seen;
                if (bucket.has(key)) return;
                bucket.add(key);
                if (target === 'vertical') verticalTopics.push(t);
                else phrases.push(t);
            };

            let listCount = 0;
            for (const li of document.querySelectorAll('main li, section li, article li, ul li, ol li')) {
                listCount++;
                add(li.textContent, 'phrase');
            }

            for (const h3 of document.querySelectorAll('h3')) {
                add(h3.textContent, 'vertical');
            }

            for (const h of document.querySelectorAll('h2,h4')) {
                add(h.textContent, 'phrase');
            }

            return JSON.stringify({ phrases, verticalTopics, listCount });
        }");

        return ParsePayload(payload);
    }

    private async Task<(IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned)> ExtractFromHttpAsync(
        string siteUrl, CancellationToken ct)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)");

        var html = await client.GetStringAsync(siteUrl, ct);
        return ExtractFromHtml(html);
    }

    internal static (IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned) ExtractFromHtml(string html)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var verticalSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var phrases = new List<string>();
        var verticalTopics = new List<string>();
        var listItemsScanned = 0;

        foreach (Match match in ListItemRegex().Matches(html))
        {
            listItemsScanned++;
            TryAddPhrase(seen, phrases, StripTags(match.Groups[1].Value));
        }

        foreach (Match match in HeadingRegex().Matches(html))
        {
            if (!int.TryParse(match.Groups[1].Value, out var level) || level is < 2 or > 4)
                continue;

            var text = StripTags(match.Groups[2].Value);
            if (level == 3)
                TryAddPhrase(verticalSeen, verticalTopics, text);
            else
                TryAddPhrase(seen, phrases, text);
        }

        return (
            phrases.Take(MaxPhrases).ToList(),
            verticalTopics.Take(MaxVerticalTopics).ToList(),
            listItemsScanned);
    }

    private static (IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned) ParsePayload(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var listCount = root.TryGetProperty("listCount", out var countEl) ? countEl.GetInt32() : 0;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var phrases = new List<string>();
        var verticalSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var verticalTopics = new List<string>();

        if (root.TryGetProperty("verticalTopics", out var verticalArr)
            && verticalArr.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in verticalArr.EnumerateArray())
            {
                if (item.ValueKind != System.Text.Json.JsonValueKind.String)
                    continue;

                TryAddPhrase(verticalSeen, verticalTopics, item.GetString() ?? string.Empty);
            }
        }

        if (root.TryGetProperty("phrases", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind != System.Text.Json.JsonValueKind.String)
                    continue;

                TryAddPhrase(seen, phrases, item.GetString() ?? string.Empty);
            }
        }

        return (
            phrases.Take(MaxPhrases).ToList(),
            verticalTopics.Take(MaxVerticalTopics).ToList(),
            listCount);
    }

    private static void TryAddPhrase(HashSet<string> seen, List<string> phrases, string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length < 4 || trimmed.Length > 80)
            return;

        if (NoisePaths.H2Noise.Contains(trimmed))
            return;

        var slug = NicheAnalyzerService.NameToSlug(trimmed);
        if (string.IsNullOrWhiteSpace(slug) || NoisePaths.IsNoise(slug))
            return;

        if (!seen.Add(trimmed))
            return;

        phrases.Add(trimmed);
    }

    private static string StripTags(string value)
    {
        var stripped = TagRegex().Replace(value, " ")
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return WebUtility.HtmlDecode(stripped).Trim();
    }

    [GeneratedRegex("<li[^>]*>([\\s\\S]*?)</li>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemRegex();

    [GeneratedRegex("<h([2-4])(?:\\s[^>]*)?>([\\s\\S]*?)</h\\1>", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
