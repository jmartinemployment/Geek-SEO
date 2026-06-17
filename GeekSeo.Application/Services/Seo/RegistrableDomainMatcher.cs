namespace GeekSeo.Application.Services.Seo;

/// <summary>v1 host check: same registrable domain (apex + subdomains on project domain).</summary>
public static class RegistrableDomainMatcher
{
    public static bool SameRegistrableDomain(string pageUrl, string projectUrl)
    {
        var pageHost = ExtractHost(pageUrl);
        var projectHost = ExtractHost(projectUrl);
        if (pageHost is null || projectHost is null)
            return false;

        pageHost = StripWww(pageHost);
        projectHost = StripWww(projectHost);

        return string.Equals(pageHost, projectHost, StringComparison.OrdinalIgnoreCase)
            || pageHost.EndsWith('.' + projectHost, StringComparison.OrdinalIgnoreCase)
            || projectHost.EndsWith('.' + pageHost, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractHost(string url)
    {
        var normalized = UrlPageKeywordResolver.NormalizeUrl(url);
        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ? uri.Host : null;
    }

    private static string StripWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
}
