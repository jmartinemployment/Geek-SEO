namespace SiteAnalyzer2.Services.Crawling;

public static class CrawlPriorityMatcher
{
    public static bool IsPriorityUrl(Uri url, IReadOnlyList<string> patterns, IReadOnlySet<string> navLinkUrls)
    {
        var normalized = NormalizeUrl(url);
        if (navLinkUrls.Contains(normalized))
            return true;

        var path = url.AbsolutePath.ToLowerInvariant();
        return patterns.Any(pattern =>
            path.Contains(pattern.ToLowerInvariant(), StringComparison.Ordinal));
    }

    public static string NormalizeUrl(Uri uri) =>
        uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
}
