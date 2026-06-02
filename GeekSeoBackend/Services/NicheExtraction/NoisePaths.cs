namespace GeekSeoBackend.Services.NicheExtraction;

internal static class NoisePaths
{
    private static readonly HashSet<string> ExactSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "about-us", "contact", "contact-us", "privacy", "privacy-policy",
        "terms", "terms-of-service", "terms-and-conditions", "legal", "disclaimer",
        "cart", "checkout", "account", "login", "register", "signup", "sign-up",
        "wp-admin", "wp-content", "wp-includes", "wp-login", "wp-json",
        "feed", "rss", "sitemap", "sitemap-index", "robots",
        "search", "404", "error", "not-found",
        "author", "page", "tag", "category", "archive",
        "testimonials", "reviews", "team", "careers", "jobs", "press",
        "faq", "help", "support", "news", "events", "gallery", "media",
        "cdn-cgi",
    };

    // Segments that start with these are also noise
    private static readonly string[] PrefixNoise =
    [
        "wp-", "admin", "cdn", "__",
    ];

    // Segments explicitly excluded from ever becoming pillars (shown in H2 filter too)
    internal static readonly HashSet<string> H2Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "why choose us", "our process", "testimonials", "about us", "our team",
        "contact us", "get a quote", "faq", "meet the team", "our mission",
        "what we do", "how it works", "our values", "get started", "learn more",
        "read more", "view all", "see more", "featured", "latest",
    };

    public static bool IsNoise(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment)) return true;
        if (ExactSegments.Contains(segment)) return true;
        foreach (var prefix in PrefixNoise)
        {
            if (segment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
