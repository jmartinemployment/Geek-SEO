using System.Text.Json;
using GeekSeo.Application.Models.Seo;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Crawls competitor sites (bounded BFS, max 50 pages each) and extracts
/// topic signals — headings, word count, FAQ schema — across their full site.
/// Deduplicates by domain so each competitor is crawled once across all pillars.
/// </summary>
public sealed class CompetitorPageFetcher(
    SitePageCrawler crawler,
    ILogger<CompetitorPageFetcher> logger)
{
    private const int MaxPagesPerCompetitor = 50;

    /// <summary>
    /// Crawl all unique competitor domains, return insights keyed by domain.
    /// </summary>
    public async Task<Dictionary<string, CompetitorSiteInsight>> CrawlCompetitorsAsync(
        IEnumerable<string> domains,
        IBrowser? browser,
        CancellationToken ct)
    {
        var results = new Dictionary<string, CompetitorSiteInsight>(StringComparer.OrdinalIgnoreCase);
        var unique = domains.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var domain in unique)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var siteUrl = domain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? domain : $"https://{domain}";

                logger.LogDebug("Crawling competitor site: {Domain}", domain);
                var crawl = await crawler.CrawlAsync(siteUrl, [], browser, ct, maxPages: MaxPagesPerCompetitor);

                var allHeadings = new List<string>();
                var totalWords = 0;
                var hasFaqSchema = false;
                var pageCount = crawl.Pages.Count;

                foreach (var page in crawl.Pages)
                {
                    var headings = ExtractHeadings(page.Html);
                    allHeadings.AddRange(headings);
                    totalWords += CountWords(StripContent(page.Html));
                    if (!hasFaqSchema)
                        hasFaqSchema = HasFaqSchema(page.Html);
                }

                var avgWordCount = pageCount > 0 ? totalWords / pageCount : 0;
                var topHeadings = allHeadings
                    .Where(h => !string.IsNullOrWhiteSpace(h))
                    .GroupBy(h => h, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .Take(30)
                    .ToList();

                results[domain] = new CompetitorSiteInsight(
                    Domain: domain,
                    PagesCrawled: pageCount,
                    AvgWordCount: avgWordCount,
                    TopHeadings: topHeadings,
                    HasFaqSchema: hasFaqSchema);

                logger.LogInformation("Competitor {Domain}: {Pages} pages, {Words} avg words, {Headings} unique headings",
                    domain, pageCount, avgWordCount, topHeadings.Count);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Competitor crawl failed for {Domain}", domain);
                results[domain] = new CompetitorSiteInsight(domain, 0, 0, [], false);
            }
        }

        return results;
    }

    private static List<string> ExtractHeadings(string html)
    {
        var list = new List<string>();
        var inTag = false;
        // Simple regex-free extraction using span scanning would be ideal,
        // but for now delegate to string search
        var pos = 0;
        while (pos < html.Length)
        {
            var h = html.IndexOf("<h", pos, StringComparison.OrdinalIgnoreCase);
            if (h < 0) break;

            if (h + 3 >= html.Length || !char.IsDigit(html[h + 2])) { pos = h + 1; continue; }
            var level = html[h + 2] - '0';
            if (level is < 1 or > 3) { pos = h + 1; continue; }

            var tagEnd = html.IndexOf('>', h);
            if (tagEnd < 0) break;

            var closeTag = $"</h{level}>";
            var closePos = html.IndexOf(closeTag, tagEnd, StringComparison.OrdinalIgnoreCase);
            if (closePos < 0) { pos = tagEnd; continue; }

            var raw = html[(tagEnd + 1)..closePos];
            var text = StripTags(raw).Trim();
            if (!string.IsNullOrWhiteSpace(text) && text.Length < 200)
                list.Add(text);

            pos = closePos + closeTag.Length;
        }
        return list;
    }

    private static bool HasFaqSchema(string html) =>
        html.Contains("FAQPage", StringComparison.OrdinalIgnoreCase) ||
        html.Contains("\"@type\":\"Question\"", StringComparison.OrdinalIgnoreCase);

    private static string StripTags(string html)
    {
        var sb = new System.Text.StringBuilder();
        var inTag2 = false;
        foreach (var c in html)
        {
            if (c == '<') inTag2 = true;
            else if (c == '>') inTag2 = false;
            else if (!inTag2) sb.Append(c);
        }
        return sb.ToString().Replace("&nbsp;", " ").Replace("&amp;", "&").Trim();
    }

    private static string StripContent(string html)
    {
        // Remove scripts and styles before word counting
        var s = System.Text.RegularExpressions.Regex.Replace(html, "<script[\\s\\S]*?</script>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        s = System.Text.RegularExpressions.Regex.Replace(s, "<style[\\s\\S]*?</style>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return StripTags(s);
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0
            : text.Split((char[])[' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
}

public sealed record CompetitorSiteInsight(
    string Domain,
    int PagesCrawled,
    int AvgWordCount,
    IReadOnlyList<string> TopHeadings,
    bool HasFaqSchema);
