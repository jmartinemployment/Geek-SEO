using System.Text.Json;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Reads document title, meta description, and H1–H6 from the homepage (HTTP or Playwright).
/// </summary>
public sealed partial class HomepageHeadingsExtractor(
    IHttpClientFactory factory,
    ILogger<HomepageHeadingsExtractor> logger)
{
    public async Task<HomepageHeadings> ExtractAsync(
        string siteUrl, IBrowser? browser, CancellationToken ct)
    {
        if (browser is not null)
        {
            try
            {
                return await ExtractWithPlaywrightAsync(siteUrl, browser, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Playwright heading extraction failed for {Url}, falling back to HTTP",
                    siteUrl);
            }
        }

        return await ExtractFromHttpAsync(siteUrl, ct);
    }

    private async Task<HomepageHeadings> ExtractWithPlaywrightAsync(
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
            const title = document.title || null;
            const meta = document.querySelector('meta[name=""description""]');
            const description = meta ? meta.getAttribute('content') : null;
            const headings = Array.from(document.querySelectorAll('h1,h2,h3,h4,h5,h6'))
                .map(el => ({
                    level: Number(el.tagName.substring(1)),
                    text: (el.textContent || '').trim(),
                }))
                .filter(h => h.text.length > 0);
            return JSON.stringify({ title, description, headings });
        }");

        return ParsePayload(payload);
    }

    private async Task<HomepageHeadings> ExtractFromHttpAsync(string siteUrl, CancellationToken ct)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (compatible; GeekSEO/1.0; +https://seo.geekatyourspot.com)");

        var html = await client.GetStringAsync(siteUrl, ct);
        var title = ExtractTitle(html);
        var description = ExtractMetaDescription(html);
        var headings = ExtractHeadingsFromHtml(html);

        return new HomepageHeadings
        {
            Title = title,
            MetaDescription = description,
            Headings = headings,
            H2Texts = headings.Where(h => h.Level == 2).Select(h => h.Text).ToList(),
        };
    }

    private static HomepageHeadings ParsePayload(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var title = root.TryGetProperty("title", out var titleEl) && titleEl.ValueKind == JsonValueKind.String
            ? titleEl.GetString()
            : null;
        var description = root.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
            ? descEl.GetString()
            : null;

        var headings = new List<PageHeading>();
        if (root.TryGetProperty("headings", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                if (!item.TryGetProperty("level", out var levelEl) ||
                    !item.TryGetProperty("text", out var textEl))
                    continue;

                var level = levelEl.GetInt32();
                var text = textEl.GetString()?.Trim();
                if (level is < 1 or > 6 || string.IsNullOrWhiteSpace(text))
                    continue;

                headings.Add(new PageHeading { Level = level, Text = text });
            }
        }

        return new HomepageHeadings
        {
            Title = title,
            MetaDescription = description,
            Headings = headings,
            H2Texts = headings.Where(h => h.Level == 2).Select(h => h.Text).ToList(),
        };
    }

    private static string? ExtractTitle(string html)
    {
        var match = TitleRegex().Match(html);
        return match.Success ? StripTags(match.Groups[1].Value).Trim() : null;
    }

    private static string? ExtractMetaDescription(string html)
    {
        var match = MetaDescriptionRegex().Match(html);
        if (!match.Success)
            match = MetaDescriptionAltRegex().Match(html);
        return match.Success ? StripTags(match.Groups[1].Value).Trim() : null;
    }

    private static List<PageHeading> ExtractHeadingsFromHtml(string html)
    {
        var list = new List<PageHeading>();
        foreach (Match match in HeadingRegex().Matches(html))
        {
            if (!int.TryParse(match.Groups[1].Value, out var level) || level is < 1 or > 6)
                continue;

            var text = StripTags(match.Groups[2].Value).Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            list.Add(new PageHeading { Level = level, Text = text });
        }

        return list;
    }

    private static string StripTags(string value) =>
        TagRegex().Replace(value, " ").Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase).Trim();

    [GeneratedRegex("<title[^>]*>([^<]*)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<meta[^>]+name=[\"']description[\"'][^>]+content=[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex MetaDescriptionRegex();

    [GeneratedRegex("<meta[^>]+content=[\"']([^\"']*)[\"'][^>]+name=[\"']description[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex MetaDescriptionAltRegex();

    [GeneratedRegex("<h([1-6])(?:\\s[^>]*)?>([\\s\\S]*?)</h\\1>", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
