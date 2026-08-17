namespace GeekSeoBackend.Extensions;

public static class CorsOriginParser
{
    private static readonly string[] DefaultOrigins =
    [
        "http://localhost:3000",
        "http://localhost:3003",
        "https://seo.geekatyourspot.com",
        "https://geek-content-creator.vercel.app",
    ];

    /// <summary>
    /// Reads comma-separated absolute origins from CORS_ORIGINS, or returns defaults when unset.
    /// </summary>
    public static string[] GetAllowedOrigins()
    {
        var raw = Environment.GetEnvironmentVariable("CORS_ORIGINS");
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultOrigins;

        var parsed = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeOrigin)
            .Where(o => Uri.TryCreate(o, UriKind.Absolute, out var uri)
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

        // Env can add origins; it cannot drop the known frontends (Vercel GCC, SEO UI, local).
        return parsed
            .Concat(DefaultOrigins)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeOrigin(string origin) =>
        origin.EndsWith('/') ? origin[..^1] : origin;
}
