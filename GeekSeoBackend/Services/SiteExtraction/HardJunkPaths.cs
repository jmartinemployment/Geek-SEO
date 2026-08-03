namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Hard-junk crawl skip list — assets, auth/cart plumbing, feeds, search, CDN internals.
/// This is intentionally NOT <see cref="NoisePaths"/>: utility pages like /about, /contact,
/// /privacy, /faq are real inventory (sitemap + future Site Audit) and must be crawled even
/// though they are excluded from pillar/topic selection. Used by both <see cref="SitePageCrawler"/>
/// and <see cref="SitemapGenerator"/> so the two share one crawl-skip definition.
/// </summary>
internal static class HardJunkPaths
{
    private static readonly HashSet<string> ExactSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "cart", "checkout", "account", "login", "register", "signup", "sign-up", "logout",
        "wp-admin", "wp-login", "wp-json",
        "feed", "rss", "search",
        "cdn-cgi",
    };

    private static readonly string[] PrefixNoise =
    [
        "wp-admin", "wp-login", "cdn-cgi", "__",
    ];

    public static bool IsHardJunk(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return false;

        if (ExactSegments.Contains(segment))
            return true;

        foreach (var prefix in PrefixNoise)
        {
            if (segment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
