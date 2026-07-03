using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Raw HTML checks before parsing manual citation lane SERP saves.
/// </summary>
public static class CitationLaneHtmlPreflight
{
    private static readonly Regex HttpUrl = new(
        @"https?://[^\s""'<>\\]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? ValidateWiki(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "Request body must contain saved Google SERP HTML.";

        var urls = ExtractHttpUrls(html);
        if (urls.Count == 0)
        {
            return "No wikipedia.org URLs in this file. Re-run Google with site:en.wikipedia.org, then save Webpage, HTML only.";
        }

        if (urls.Any(HasNonWikipediaWikiTld))
        {
            return "Wrong wiki SERP: this file has .wiki sites (e.g. aisdr.wiki), not en.wikipedia.org."
                + " Use Google site:en.wikipedia.org and save Webpage, HTML only.";
        }

        if (!urls.Any(url => CitationLaneHostRules.IsEligibleUrl(url, SerpResearchLanes.Wiki)))
        {
            return "No wikipedia.org URLs in this file. Re-run Google with site:en.wikipedia.org, then save Webpage, HTML only.";
        }

        return null;
    }

    internal static List<string> ExtractHttpUrls(string html)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(html);
        var urls = new List<string>();
        foreach (Match match in HttpUrl.Matches(decoded))
        {
            var raw = match.Value.TrimEnd('"', '\'', ',', ';', ')', ']', '}', '>');
            if (Uri.TryCreate(raw, UriKind.Absolute, out _))
                urls.Add(raw);
        }

        return urls
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool HasNonWikipediaWikiTld(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return CitationLaneHostRules.IsNonWikipediaWikiTld(uri.Host);
    }
}
