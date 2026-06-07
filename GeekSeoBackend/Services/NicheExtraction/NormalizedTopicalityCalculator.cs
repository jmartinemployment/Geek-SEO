using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Estimates per-pillar normalized topicality: share of crawled site content (by word count)
/// attributed to each selected pillar — mirrors Google's NormalizedTopicality signal (Gap 4).
/// </summary>
public static partial class NormalizedTopicalityCalculator
{
    private const decimal MinMatchScore = 0.25m;
    private const int MinPageWords = 50;

    internal static SiteTopicProfile Apply(
        SiteTopicProfile fused,
        SiteCrawlData crawl,
        UrlPatternData urlPatterns)
    {
        if (fused.SelectedPillars.Count == 0 || crawl.Pages.Count == 0)
        {
            return fused with { NormalizedTopicalityBySlug = new Dictionary<string, decimal>() };
        }

        var slugToUrl = urlPatterns.Topics.ToDictionary(
            t => t.Slug,
            t => t.Url,
            StringComparer.OrdinalIgnoreCase);

        var rawWeight = fused.SelectedPillars.ToDictionary(
            p => p.Slug,
            _ => 0m,
            StringComparer.OrdinalIgnoreCase);

        var totalPageWeight = 0m;

        foreach (var page in crawl.Pages)
        {
            var pageWeight = EstimateWordCount(page.Html);
            if (pageWeight < MinPageWords)
                pageWeight = MinPageWords;

            totalPageWeight += pageWeight;

            var best = ScorePageAgainstPillars(page, fused.SelectedPillars, slugToUrl);
            if (best.Score < MinMatchScore)
                continue;

            rawWeight[best.Slug] += pageWeight * best.Score;
        }

        var normalized = Normalize(rawWeight, totalPageWeight);
        return fused with { NormalizedTopicalityBySlug = normalized };
    }

    internal static (string Slug, decimal Score) ScorePageAgainstPillars(
        CrawledPage page,
        IReadOnlyList<TopicCandidate> pillars,
        IReadOnlyDictionary<string, string> slugToUrl)
    {
        var path = SafePath(page.Url);
        var text = VisibleText(page.Html);

        var bestSlug = string.Empty;
        var bestScore = 0m;

        foreach (var pillar in pillars)
        {
            var score = ScorePageForPillar(page.Url, path, text, pillar, slugToUrl);
            if (score > bestScore)
            {
                bestScore = score;
                bestSlug = pillar.Slug;
            }
        }

        return (bestSlug, bestScore);
    }

    private static decimal ScorePageForPillar(
        string pageUrl,
        string path,
        string visibleText,
        TopicCandidate pillar,
        IReadOnlyDictionary<string, string> slugToUrl)
    {
        if (!string.IsNullOrWhiteSpace(pillar.DedicatedPageUrl)
            && UrlsMatch(pageUrl, pillar.DedicatedPageUrl))
            return 1.0m;

        if (slugToUrl.TryGetValue(pillar.Slug, out var patternUrl)
            && UrlsMatch(pageUrl, patternUrl))
            return 0.95m;

        if (PathMatchesSlug(path, pillar.Slug))
            return 0.9m;

        var name = pillar.Name.Trim();
        if (name.Length >= 3
            && visibleText.Contains(name, StringComparison.OrdinalIgnoreCase))
            return 0.45m;

        var slugPhrase = pillar.Slug.Replace('-', ' ');
        if (slugPhrase.Length >= 3
            && visibleText.Contains(slugPhrase, StringComparison.OrdinalIgnoreCase))
            return 0.35m;

        return 0m;
    }

    private static IReadOnlyDictionary<string, decimal> Normalize(
        Dictionary<string, decimal> rawWeight,
        decimal totalPageWeight)
    {
        if (totalPageWeight <= 0)
        {
            return rawWeight.ToDictionary(kv => kv.Key, _ => 0m, StringComparer.OrdinalIgnoreCase);
        }

        return rawWeight.ToDictionary(
            kv => kv.Key,
            kv => Math.Round(kv.Value / totalPageWeight, 4),
            StringComparer.OrdinalIgnoreCase);
    }

    internal static int EstimateWordCount(string html)
    {
        return VisibleText(html)
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static string VisibleText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var stripped = ScriptTagRegex().Replace(html, " ");
        stripped = StyleTagRegex().Replace(stripped, " ");
        stripped = TagRegex().Replace(stripped, " ");
        return WebUtility.HtmlDecode(stripped);
    }

    private static string SafePath(string url)
    {
        try
        {
            return new Uri(url).AbsolutePath;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool PathMatchesSlug(string path, string slug)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(slug))
            return false;

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(s =>
            s.Equals(slug, StringComparison.OrdinalIgnoreCase)
            || s.Replace('-', ' ').Equals(slug.Replace('-', ' '), StringComparison.OrdinalIgnoreCase));
    }

    private static bool UrlsMatch(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        try
        {
            var left = new Uri(a);
            var right = new Uri(b);
            return string.Equals(
                left.GetLeftPart(UriPartial.Path).TrimEnd('/'),
                right.GetLeftPart(UriPartial.Path).TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex("<script\\b[^>]*>[\\s\\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex("<style\\b[^>]*>[\\s\\S]*?</style>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleTagRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();
}
