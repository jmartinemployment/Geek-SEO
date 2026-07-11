namespace GeekSeo.Application.Services.Seo;

public static class CitationDomainSourceResolver
{
    public static string Resolve(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return "unknown";

        var host = uri.Host.ToLowerInvariant();
        if (host.EndsWith(".gov", StringComparison.Ordinal))
            return "government";

        if (host.EndsWith(".edu", StringComparison.Ordinal))
            return "research";

        if (host.Contains("wikipedia.org", StringComparison.Ordinal))
            return "wikipedia";

        return "unknown";
    }
}
