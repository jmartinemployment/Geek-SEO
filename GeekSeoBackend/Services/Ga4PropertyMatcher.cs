namespace GeekSeoBackend.Services;

public sealed record Ga4PropertyCandidate(
    string PropertyId,
    string DisplayName,
    IReadOnlyList<string> WebDefaultUris);

/// <summary>
/// Maps a project URL to a GA4 property the connected Google account can read.
/// </summary>
public static class Ga4PropertyMatcher
{
    public static string? Match(
        IReadOnlyList<Ga4PropertyCandidate> candidates,
        string? projectSiteUrl,
        string? preferredPropertyId)
    {
        if (candidates.Count == 0)
            return null;

        var normalizedPreferred = NormalizePropertyId(preferredPropertyId);
        if (!string.IsNullOrWhiteSpace(normalizedPreferred)
            && candidates.Any(c => c.PropertyId == normalizedPreferred)
            && candidates.Where(c => SiteMatches(c, projectSiteUrl)).Any(c => c.PropertyId == normalizedPreferred))
        {
            return normalizedPreferred;
        }

        var siteMatches = candidates.Where(c => SiteMatches(c, projectSiteUrl)).ToList();
        if (siteMatches.Count == 1)
            return siteMatches[0].PropertyId;

        if (siteMatches.Count > 1 && !string.IsNullOrWhiteSpace(normalizedPreferred))
        {
            var preferredSiteMatch = siteMatches.FirstOrDefault(c => c.PropertyId == normalizedPreferred);
            if (preferredSiteMatch is not null)
                return preferredSiteMatch.PropertyId;
        }

        if (siteMatches.Count > 0)
            return siteMatches[0].PropertyId;

        if (!string.IsNullOrWhiteSpace(normalizedPreferred)
            && candidates.Any(c => c.PropertyId == normalizedPreferred))
        {
            return normalizedPreferred;
        }

        return candidates[0].PropertyId;
    }

    public static string NormalizePropertyId(string? propertyId)
    {
        if (string.IsNullOrWhiteSpace(propertyId))
            return string.Empty;

        var trimmed = propertyId.Trim();
        const string prefix = "properties/";
        if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[prefix.Length..];

        return trimmed.Split('/').FirstOrDefault() ?? trimmed;
    }

    internal static bool SiteMatches(Ga4PropertyCandidate candidate, string? projectSiteUrl)
    {
        if (string.IsNullOrWhiteSpace(projectSiteUrl))
            return false;

        var projectHost = NormalizeHost(HostFromUrl(projectSiteUrl));
        if (projectHost is null)
            return false;

        foreach (var uri in candidate.WebDefaultUris)
        {
            var streamHost = NormalizeHost(HostFromUrl(uri));
            if (streamHost is not null && streamHost == projectHost)
                return true;
        }

        var display = candidate.DisplayName.ToLowerInvariant();
        return display.Contains(projectHost, StringComparison.Ordinal)
            || (projectHost.StartsWith("www.", StringComparison.Ordinal)
                && display.Contains(projectHost[4..], StringComparison.Ordinal));
    }

    private static string? HostFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        return uri.Host;
    }

    private static string? NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var normalized = host.Trim().ToLowerInvariant();
        return normalized.StartsWith("www.", StringComparison.Ordinal)
            ? normalized[4..]
            : normalized;
    }
}
