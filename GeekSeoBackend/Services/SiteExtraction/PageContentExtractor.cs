using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Extracts service-like phrases from homepage body content (lists + section headings).
/// </summary>
public sealed partial class PageContentExtractor
{
    public async Task<PageContentData> ExtractAsync(string domain, IBrowser? browser, CancellationToken ct)
    {
        // See HomepageHeadingsExtractor — the HTTP/regex path is lower-fidelity; degrading to it
        // silently would poison downstream heading-gap detection. Fail closed instead.
        if (browser is null)
            throw new InvalidOperationException(
                $"Playwright browser unavailable — cannot extract real page content for {domain}.");

        var (phrases, verticalTopics, listCount) = await ExtractWithPlaywrightAsync(domain, browser, ct);
        return new PageContentData(phrases, verticalTopics, listCount);
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

              document.querySelectorAll('h1,h2,h3,h4,h5,h6').forEach(h => {
                const level = parseInt(h.tagName.substring(1), 10);
                const text = (h.textContent || '').replace(/\s+/g, ' ').trim();
                if (text) result.headings.push({ level, text });
              });

              // ul/ol list items are not headings — keep without length/Noise filter if needed, but not as headings
              document.querySelectorAll('main li, article li, section li, ul li, ol li').forEach(li => {
                const text = (li.textContent || '').replace(/\s+/g, ' ').trim();
                if (text) add(result.listItems, text);
              });

              return JSON.stringify(result);
            }
            """);

        return ParsePayload(json);
    }

    internal static (IReadOnlyList<string> Phrases, IReadOnlyList<string> VerticalTopics, int ListItemsScanned) ExtractFromHtml(string html)
    {
        var orderedHeadings = ExtractOrderedHeadings(html);
        var (phrases, verticalTopics) = ClassifyHeadings(orderedHeadings);

        var listItems = new List<string>();
        foreach (Match match in ListItemRegex().Matches(html))
        {
            var text = WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
            text = TagStripRegex().Replace(text, " ").Trim();
            if (!string.IsNullOrWhiteSpace(text))
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
            if (NoisePaths.IsNoise(SiteAnalyzerService.NameToSlug(item)))
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
