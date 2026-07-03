using System.Net;
using System.Text.RegularExpressions;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Serp;

/// <summary>
/// When the primary SERP parser finds no citation-lane URLs, scan saved HTML for eligible links.
/// </summary>
public static class CitationLaneHtmlFallback
{
    private static readonly Regex ModernResultHref = new(
        @"jsname=""UWckNb""[^>]*href=""([^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AbsoluteHttpUrl = new(
        @"https?://[^\s""'<>\\]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static SerpLivePageParseResult Enrich(
        SerpLivePageParseResult parsed,
        string html,
        string lane)
    {
        if (!IsCitationLane(lane) || HasEligible(parsed, lane))
            return parsed;

        var urls = ExtractUrls(html, lane);
        if (urls.Count == 0)
            return parsed;

        var rankAbsolute = parsed.Items.Count > 0
            ? parsed.Items.Max(i => i.RankAbsolute) + 1
            : 1;
        var rankGroup = parsed.Items.Count(i =>
                string.Equals(i.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase))
            + 1;
        var page = parsed.PagesCount;
        var existing = new HashSet<string>(
            parsed.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                .Select(i => i.Url!),
            StringComparer.OrdinalIgnoreCase);

        var extras = new List<SerpParsedItem>();
        foreach (var url in urls)
        {
            if (!existing.Add(url))
                continue;

            var domain = TryGetDomain(url);
            extras.Add(new SerpParsedItem(
                SerpItemTypes.Organic,
                rankGroup,
                rankAbsolute,
                page,
                Domain: domain,
                Title: TitleFromUrl(url) ?? domain ?? url,
                Url: url));
            rankGroup++;
            rankAbsolute++;
        }

        if (extras.Count == 0)
            return parsed;

        var merged = parsed.Items.Concat(extras).ToList();
        var types = merged
            .Select(i => i.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return parsed with { Items = merged, ItemTypes = types };
    }

    private static bool IsCitationLane(string lane) =>
        string.Equals(lane, SerpResearchLanes.Wiki, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lane, SerpResearchLanes.Gov, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lane, SerpResearchLanes.Edu, StringComparison.OrdinalIgnoreCase);

    private static bool HasEligible(SerpLivePageParseResult parsed, string lane) =>
        parsed.Items.Any(i =>
            string.Equals(i.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase)
            && !i.Ads
            && !string.IsNullOrWhiteSpace(i.Url)
            && CitationLaneDomainRules.IsEligibleUrl(i.Url!, lane));

    private static List<string> ExtractUrls(string html, string lane)
    {
        var decoded = WebUtility.HtmlDecode(html);
        var urls = new List<string>();

        foreach (Match match in ModernResultHref.Matches(decoded))
            TryAddUrl(urls, match.Groups[1].Value, lane);

        foreach (Match match in AbsoluteHttpUrl.Matches(decoded))
        {
            var raw = match.Value.TrimEnd('"', '\'', ',', ';', ')', ']', '}', '>');
            TryAddUrl(urls, raw, lane);
        }

        return urls
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();
    }

    private static void TryAddUrl(List<string> urls, string? href, string lane)
    {
        var normalized = SerpResultUrlNormalizer.Normalize(href);
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (!CitationLaneDomainRules.IsEligibleUrl(normalized, lane))
            return;

        if (GoogleSerpHtmlParser.ShouldSkipResultUrlPublic(normalized))
            return;

        urls.Add(normalized);
    }

    private static string? TitleFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var path = uri.AbsolutePath;
        var wikiIdx = path.IndexOf("/wiki/", StringComparison.OrdinalIgnoreCase);
        if (wikiIdx >= 0)
        {
            var slug = path[(wikiIdx + "/wiki/".Length)..];
            slug = Uri.UnescapeDataString(slug).Replace('_', ' ');
            return string.IsNullOrWhiteSpace(slug) ? null : slug;
        }

        return uri.Host;
    }

    private static string? TryGetDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;
    }
}
