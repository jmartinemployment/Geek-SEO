namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Classifies schema.org sameAs URLs into known entity authority platforms (no HTTP lookups).
/// </summary>
internal static class SameAsClassifier
{
    internal static IReadOnlyList<string> ResolvePlatforms(IReadOnlyList<string> sameAsUrls)
    {
        var platforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in sameAsUrls)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri))
                continue;

            var host = uri.Host.ToLowerInvariant();
            var platform = ClassifyHost(host, uri.AbsolutePath);
            if (platform is not null)
                platforms.Add(platform);
        }

        return platforms.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal static bool IsEntityResolved(IReadOnlyList<string> platforms) =>
        platforms.Count > 0;

    private static string? ClassifyHost(string host, string path)
    {
        if (host.Contains("wikipedia.org", StringComparison.Ordinal))
            return "wikipedia";

        if (host.Contains("wikidata.org", StringComparison.Ordinal))
            return "wikidata";

        if (host.Contains("linkedin.com", StringComparison.Ordinal))
            return "linkedin";

        if (host is "www.google.com" or "google.com" && path.Contains("/maps", StringComparison.Ordinal))
            return "google_business";

        if (host.Contains("facebook.com", StringComparison.Ordinal) || host.Contains("fb.com", StringComparison.Ordinal))
            return "facebook";

        if (host.Contains("twitter.com", StringComparison.Ordinal) || host is "x.com")
            return "twitter";

        if (host.Contains("instagram.com", StringComparison.Ordinal))
            return "instagram";

        if (host.Contains("youtube.com", StringComparison.Ordinal))
            return "youtube";

        if (host.Contains("crunchbase.com", StringComparison.Ordinal))
            return "crunchbase";

        if (host.Contains("bbb.org", StringComparison.Ordinal))
            return "bbb";

        if (host.Contains("yelp.com", StringComparison.Ordinal))
            return "yelp";

        return null;
    }
}
