namespace SiteAnalyzer2.Services.Integrations;

/// <summary>
/// Canonical project URL matching <c>site_profiles."SiteUrl"</c>:
/// https://www.{host}/ — always https, www, lowercase host, trailing slash, no path/query.
/// </summary>
public static class TargetSiteUrlNormalizer
{
    /// <summary>
    /// Mirrors <c>chk_site_profiles_siteurl_format</c>:
    /// <c>LIKE 'https://www.%'</c>, trailing <c>/</c>, all lowercase.
    /// </summary>
    public static bool IsValidStoredFormat(string? siteUrl) =>
        !string.IsNullOrEmpty(siteUrl)
        && siteUrl.StartsWith("https://www.", StringComparison.Ordinal)
        && siteUrl.EndsWith("/", StringComparison.Ordinal)
        && string.Equals(siteUrl, siteUrl.ToLowerInvariant(), StringComparison.Ordinal);

    public static string Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim();
        if (!trimmed.Contains("://", StringComparison.Ordinal))
            trimmed = "https://" + trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            return string.Empty;

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];

        var port = uri is { IsDefaultPort: false, Port: var p } ? $":{p}" : string.Empty;

        var normalized = $"https://www.{host}{port}/";
        return IsValidStoredFormat(normalized) ? normalized : string.Empty;
    }

    public static bool Equals(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
}
