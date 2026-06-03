namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Normalizes project/site URLs for HTTP and Playwright (trim, single scheme, authority only).
/// </summary>
internal static class NicheSiteUrlNormalizer
{
    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Site URL is required.");

        var value = raw.Trim();

        foreach (var part in value.Split([' '], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!part.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                continue;

            if (Uri.TryCreate(part.TrimEnd('/'), UriKind.Absolute, out var parsed)
                && !string.IsNullOrWhiteSpace(parsed.Host))
            {
                return parsed.GetLeftPart(UriPartial.Authority);
            }
        }

        if (!value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            value = "https://" + value.TrimStart('/');

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException($"Invalid site URL: {raw}");

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
