namespace GeekSeo.Application.Services.Seo;

public static class AuthoritativeCitationRules
{
    private static readonly HashSet<string> TrustedPublisherHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "hbr.org",
        "forbes.com",
        "gartner.com",
        "mckinsey.com",
        "nist.gov",
        "who.int",
        "w3.org",
        "ietf.org",
        "iso.org",
        "nature.com",
        "sciencedirect.com",
        "pubmed.ncbi.nlm.nih.gov",
        "ncbi.nlm.nih.gov",
        "en.wikipedia.org",
        "wikipedia.org",
    };

    private static readonly string[] BlockedHostFragments =
    [
        "reddit.",
        "quora.",
        "pinterest.",
        "facebook.",
        "twitter.",
        "x.com",
        "medium.com",
        "youtube.",
        "tiktok.",
        "scholar.google.",
    ];

    public static bool IsAuthoritativeCitationUrl(string url)
    {
        if (!TryGetHost(url, out var host))
            return false;

        return HasAuthoritativeTld(host) || TrustedPublisherHosts.Contains(host);
    }

    public static bool IsAcceptableDiscoveredCitationUrl(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!TryGetHost(url, out var host) || IsBlockedHost(host))
            return false;

        return IsAuthoritativeCitationUrl(url);
    }

    public static IReadOnlyList<T> FilterSerpCitationPicks<T>(IEnumerable<T> picks, Func<T, string> urlSelector) =>
        picks.Where(pick => IsAuthoritativeCitationUrl(urlSelector(pick))).ToList();

    private static bool HasAuthoritativeTld(string host) =>
        host.EndsWith(".gov", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".edu", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".mil", StringComparison.OrdinalIgnoreCase);

    private static bool IsBlockedHost(string host)
    {
        if (CitationLaneHostRules.IsNonWikipediaWikiTld(host))
            return true;

        foreach (var fragment in BlockedHostFragments)
        {
            if (host.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool TryGetHost(string url, out string host)
    {
        host = string.Empty;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        host = uri.Host.ToLowerInvariant();
        return host.Length > 0;
    }
}
