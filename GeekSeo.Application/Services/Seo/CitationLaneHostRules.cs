namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Host eligibility for manual citation lane imports (gov, edu, wiki).
/// Rebuilt for M2+ tier — replaces dormant SiteAnalyzer2 <c>CitationLaneDomainRules</c>.
/// </summary>
public static class CitationLaneHostRules
{
    public static bool IsEligibleUrl(string url, string lane)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        return lane.ToLowerInvariant() switch
        {
            Models.Seo.SerpResearchLanes.Gov => host.EndsWith(".gov", StringComparison.Ordinal),
            Models.Seo.SerpResearchLanes.Edu => host.EndsWith(".edu", StringComparison.Ordinal),
            Models.Seo.SerpResearchLanes.Wiki => IsWikipediaHost(host),
            _ => true,
        };
    }

    public static bool IsWikipediaHost(string host) =>
        host.Equals("wikipedia.org", StringComparison.Ordinal)
        || host.EndsWith(".wikipedia.org", StringComparison.Ordinal);

    /// <summary>True for custom .wiki TLD hosts (aisdr.wiki) — not en.wikipedia.org.</summary>
    public static bool IsNonWikipediaWikiTld(string host)
    {
        var normalized = host.ToLowerInvariant();
        return normalized.EndsWith(".wiki", StringComparison.Ordinal) && !IsWikipediaHost(normalized);
    }
}
