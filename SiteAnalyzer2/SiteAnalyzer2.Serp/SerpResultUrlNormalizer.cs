namespace SiteAnalyzer2.Serp;

public static class SerpResultUrlNormalizer
{
    public static string? Normalize(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return null;

        href = href.Trim();
        if (href.StartsWith("/url?", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("https://www.google.com/url?", StringComparison.OrdinalIgnoreCase))
        {
            var query = href.Contains('?') ? href[(href.IndexOf('?') + 1)..] : href;
            foreach (var part in query.Split('&'))
            {
                if (part.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(part["q=".Length..]);
                if (part.StartsWith("url=", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(part["url=".Length..]);
            }

            return null;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.ToString();
        }

        return null;
    }
}
