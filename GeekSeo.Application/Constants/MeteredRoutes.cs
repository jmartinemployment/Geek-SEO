namespace GeekSeo.Application.Constants.Seo;

/// <summary>Maps HTTP method + path to usage feature keys (see UsageLimits.cs).</summary>
public static class MeteredRoutes
{
    private static readonly (string Method, string PathPrefix, string Feature)[] ExactPrefixes =
    [
        ("POST", "/api/seo/keywords/research", "keyword_lookup"),
        ("POST", "/api/seo/audit/page", "page_audit"),
        ("POST", "/api/seo/audit/site", "site_audit"),
        ("GET", "/api/seo/serp/deep", "deep_serp"),
        ("POST", "/api/seo/topical-map/generate", "topical_map_refresh"),
    ];

    public static string? GetFeatureKey(string method, string path)
    {
        if (!path.StartsWith("/api/seo", StringComparison.OrdinalIgnoreCase))
            return null;

        var upperMethod = method.ToUpperInvariant();

        foreach (var (routeMethod, prefix, feature) in ExactPrefixes)
        {
            if (!string.Equals(routeMethod, upperMethod, StringComparison.Ordinal))
                continue;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return feature;
        }

        return null;
    }
}
