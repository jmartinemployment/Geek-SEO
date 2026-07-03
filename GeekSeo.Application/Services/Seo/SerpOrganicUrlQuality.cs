namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Filters junk organic URLs that should not drive outlines, headings, or citations.
/// </summary>
public static class SerpOrganicUrlQuality
{
    public static bool IsUsableOrganicUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        if (CitationLaneHostRules.IsNonWikipediaWikiTld(host))
            return false;

        return true;
    }
}
