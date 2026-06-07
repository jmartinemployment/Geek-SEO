using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Extracts service-like phrases from homepage body content (lists + section headings).
/// Playwright when available; HTTP + regex fallback.
/// </summary>
public sealed partial class PageContentExtractor(
    IHttpClientFactory httpClientFactory,
    ILogger<PageContentExtractor> logger)
{
    private const int MaxListItems = 30;

    public async Task<PageContentData> ExtractAsync(string domain, IBrowser? browser, CancellationToken ct)
    {
        if (browser is not null)
        {
            try
            {
                var (phrases, verticalTopics, listCount) = await ExtractWithPlaywrightAsync(domain, browser, ct);
                return new PageContentData(phrases, verticalTopics, listCount);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Playwright page content extraction failed for {Domain}, falling back to HTTP", domain);
            }
        }

        var httpResult = await ExtractFromHttpAsync(domain, ct);
        return new PageContentData(httpResult.Phrases, httpResult.VerticalTopics, httpResult.ListItemsScanned);
    }

    private async Task<(IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned)> ExtractWithPlaywrightAsync(
        string domain,
        IBrowser browser,
        CancellationToken ct)
    {
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(domain, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });

        var json = await page.EvaluateAsync<string>(
            """
            () => {
              const result = { headings: [], listItems: [] };
              const seen = new Set();
              const add = (arr, text) => {
                const t = (text || '').replace(/\s+/g, ' ').trim();
                if (t.length < 4 || t.length > 80) return;
                const key = t.toLowerCase();
                if (seen.has(key)) return;
                seen.add(key);
                arr.push(t);
              };

              document.querySelectorAll('h1,h2,h3,h4').forEach(h => {
                const level = parseInt(h.tagName.substring(1), 10);
                const text = (h.textContent || '').replace(/\s+/g, ' ').trim();
                if (text) result.headings.push({ level, text });
              });

              document.querySelectorAll('main li, article li, section li, ul li, ol li').forEach(li => {
                if (result.listItems.length >= 30) return;
                const text = (li.textContent || '').replace(/\s+/g, ' ').trim();
                if (text.length >= 4 && text.length <= 80) add(result.listItems, text);
              });

              return JSON.stringify(result);
            }
            """);

        return ParsePayload(json);
    }

    private async Task<(IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned)> ExtractFromHttpAsync(
        string domain,
        CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient("SeoFetch");
        using var response = await client.GetAsync(domain, ct);
        if (!response.IsSuccessStatusCode)
            return ([], [], 0);

        var html = await response.Content.ReadAsStringAsync(ct);
        return ExtractFromHtml(html);
    }

    internal static (IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned) ExtractFromHtml(string html)
    {
        var orderedHeadings = ExtractOrderedHeadings(html);
        var (phrases, verticalTopics) = ClassifyHeadings(orderedHeadings);

        var listItems = new List<string>();
        foreach (Match match in ListItemRegex().Matches(html))
        {
            if (listItems.Count >= MaxListItems)
                break;

            var text = WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
            text = TagStripRegex().Replace(text, " ").Trim();
            if (text.Length is >= 4 and <= 80 && !NoisePaths.IsNoise(NicheAnalyzerService.NameToSlug(text)))
                listItems.Add(text);
        }

        foreach (var item in listItems)
            phrases.Add(item);

        return (
            phrases,
            verticalTopics,
            listItems.Count);
    }

    internal static (List<string> Phrases, List<string> VerticalTopics) ClassifyHeadings(
        IReadOnlyList<(int Level, string Text)> orderedHeadings)
    {
        var phrases = new List<string>();
        var verticalTopics = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var underSectionParent = false;

        foreach (var (level, rawText) in orderedHeadings)
        {
            var text = rawText.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (level == 2 && PageVerticalClassifier.IsSectionParent(text))
            {
                underSectionParent = true;
                AddUnique(phrases, seen, text);
                continue;
            }

            if (PageVerticalClassifier.ShouldTreatAsVertical(level, text, underSectionParent))
            {
                AddUnique(verticalTopics, seen, text);
                if (level == 2)
                    underSectionParent = false;
                continue;
            }

            if (PageVerticalClassifier.ResetsSectionContext(level, text))
                underSectionParent = false;

            AddUnique(phrases, seen, text);
        }

        return (phrases, verticalTopics);
    }

    private static void AddUnique(List<string> target, HashSet<string> seen, string text)
    {
        if (!seen.Add(text))
            return;

        target.Add(text);
    }

    private static List<(int Level, string Text)> ExtractOrderedHeadings(string html)
    {
        var headings = new List<(int Level, string Text)>();
        foreach (Match match in OrderedHeadingRegex().Matches(html))
        {
            if (!int.TryParse(match.Groups[1].Value, out var level))
                continue;

            var text = WebUtility.HtmlDecode(match.Groups[2].Value.Trim());
            text = TagStripRegex().Replace(text, " ").Trim();
            if (text.Length > 0)
                headings.Add((level, text));
        }

        return headings;
    }

    private static (IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned) ParsePayload(string json)
    {
        var phrases = new List<string>();
        var verticalTopics = new List<string>();
        var listItems = new List<string>();
        var orderedHeadings = new List<(int Level, string Text)>();

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("headings", out var headingsEl))
            {
                foreach (var h in headingsEl.EnumerateArray())
                {
                    if (!h.TryGetProperty("level", out var levelEl) || !h.TryGetProperty("text", out var textEl))
                        continue;

                    orderedHeadings.Add((levelEl.GetInt32(), textEl.GetString() ?? string.Empty));
                }
            }

            if (doc.RootElement.TryGetProperty("listItems", out var listEl))
            {
                foreach (var item in listEl.EnumerateArray())
                {
                    var text = item.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        listItems.Add(text);
                }
            }
        }
        catch
        {
            return ([], [], 0);
        }

        var classified = ClassifyHeadings(orderedHeadings);
        phrases.AddRange(classified.Phrases);
        verticalTopics.AddRange(classified.VerticalTopics);

        foreach (var item in listItems)
        {
            if (NoisePaths.IsNoise(NicheAnalyzerService.NameToSlug(item)))
                continue;

            phrases.Add(item);
        }

        return (
            phrases,
            verticalTopics,
            listItems.Count);
    }

    [GeneratedRegex(@"<h([234])[^>]*>(.*?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex OrderedHeadingRegex();

    [GeneratedRegex(@"<li[^>]*>(.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ListItemRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagStripRegex();
}
