namespace GeekSeo.Application.Infrastructure;

/// <summary>
/// Normalizes SEO project site URLs (trim, single scheme, authority only).
/// </summary>
public static class SeoSiteUrlNormalizer
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

    public static bool TryNormalize(string raw, out string normalized, out string error)
    {
        try
        {
            normalized = Normalize(raw);
            error = string.Empty;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            normalized = string.Empty;
            error = ex.Message;
            return false;
        }
    }
}
