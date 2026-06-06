using System.Net;
using System.Text.RegularExpressions;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Extracts same-origin anchor text and inbound link counts from crawled pages.
/// Mirrors search-engine T* topicality from internal link graph (Phase B).
/// </summary>
public sealed partial class InternalLinkExtractor
{
    public InternalLinkData Extract(SiteCrawlData crawl, string siteUrl)
    {
        if (crawl.Pages.Count == 0)
            return new InternalLinkData([], new Dictionary<string, int>(), 0);

        if (!TryGetOrigin(siteUrl, out var origin))
            return new InternalLinkData([], new Dictionary<string, int>(), crawl.Pages.Count);

        var edges = new List<InternalLinkEdge>();
        var inbound = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in crawl.Pages)
        {
            foreach (var edge in ExtractLinksFromHtml(page.Html, page.Url, origin))
            {
                edges.Add(edge);
                inbound[edge.TargetUrl] = inbound.GetValueOrDefault(edge.TargetUrl) + 1;
            }
        }

        return new InternalLinkData(edges, inbound, crawl.Pages.Count);
    }

    internal static IEnumerable<InternalLinkEdge> ExtractLinksFromHtml(
        string html, string pageUrl, string origin)
    {
        foreach (Match match in AnchorLinkRegex().Matches(html))
        {
            var href = WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
            var rawAnchor = match.Groups[2].Value;
            var anchor = StripTags(WebUtility.HtmlDecode(rawAnchor)).Replace('\n', ' ').Trim();
            anchor = CollapseWhitespace(anchor);

            if (!SitePageCrawler.TryResolveUrl(href, pageUrl, origin, out var targetUrl))
                continue;

            if (AnchorTextFilter.IsUsableTopic(anchor))
            {
                yield return new InternalLinkEdge(pageUrl, targetUrl, anchor);
                continue;
            }

            if (TryTopicFromTargetUrl(targetUrl, out var topicFromUrl))
            {
                yield return new InternalLinkEdge(
                    pageUrl,
                    targetUrl,
                    topicFromUrl,
                    InferredFromUrlSlug: true);
            }
        }
    }

    internal static bool TryTopicFromTargetUrl(string targetUrl, out string topicName)
    {
        topicName = string.Empty;
        try
        {
            var path = new Uri(targetUrl).AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(path))
                return false;

            foreach (var (_, slug) in UrlPatternExtractor.ExtractTopicSegments(path))
            {
                if (NoisePaths.IsNoise(slug))
                    continue;

                topicName = SitemapExtractor.SlugToTitle(slug);
                return topicName.Length >= 4;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool TryGetOrigin(string siteUrl, out string origin)
    {
        origin = string.Empty;
        try
        {
            origin = new Uri(NicheSiteUrlNormalizer.Normalize(siteUrl)).GetLeftPart(UriPartial.Authority);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string StripTags(string value) =>
        TagRegex().Replace(value, " ");

    private static string CollapseWhitespace(string value) =>
        WhitespaceRegex().Replace(value, " ").Trim();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(
        @"<a\s[^>]*href=[""']([^""'#][^""']*)[""'][^>]*>(.*?)</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex AnchorLinkRegex();
}
