namespace SiteAnalyzer2.Domain;

public static class CitationLaneDomainRules
{
    public static bool IsEligibleUrl(string url, string lane)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        return lane.ToLowerInvariant() switch
        {
            SerpResearchLanes.Gov => host.EndsWith(".gov", StringComparison.Ordinal),
            SerpResearchLanes.Edu => host.EndsWith(".edu", StringComparison.Ordinal),
            SerpResearchLanes.Wiki => host.Contains("wikipedia.org", StringComparison.Ordinal),
            _ => true,
        };
    }
}
