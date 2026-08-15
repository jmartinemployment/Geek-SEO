using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Extracts same-origin anchor text and inbound link counts from crawled pages.
/// Prefers structured links on the crawl record; falls back to a DOM walk (never regex).
/// </summary>
public sealed class InternalLinkExtractor
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
            foreach (var edge in ExtractLinksFromPage(page, origin))
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
        foreach (var link in SitePageCrawler.ExtractLinksFromHtml(html, pageUrl, origin))
        {
            foreach (var edge in ToEdge(pageUrl, origin, link.Href, link.Text))
                yield return edge;
        }
    }

    private static IEnumerable<InternalLinkEdge> ExtractLinksFromPage(CrawledPage page, string origin)
    {
        var links = page.Links.Count > 0
            ? page.Links
            : SitePageCrawler.ExtractLinksFromHtml(page.Html, page.Url, origin);

        foreach (var link in links)
        {
            foreach (var edge in ToEdge(page.Url, origin, link.Href, link.Text))
                yield return edge;
        }
    }

    private static IEnumerable<InternalLinkEdge> ToEdge(
        string pageUrl, string origin, string href, string anchor)
    {
        if (!SitePageCrawler.TryResolveUrl(href, pageUrl, origin, out var targetUrl))
            yield break;

        if (AnchorTextFilter.IsUsableTopic(anchor))
        {
            yield return new InternalLinkEdge(pageUrl, targetUrl, anchor);
            yield break;
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
                topicName = SitemapExtractor.SlugToTitle(slug);
                return !string.IsNullOrWhiteSpace(topicName);
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
            origin = new Uri(SiteUrlNormalizer.Normalize(siteUrl)).GetLeftPart(UriPartial.Authority);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
