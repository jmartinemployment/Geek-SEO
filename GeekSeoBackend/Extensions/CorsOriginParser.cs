namespace GeekSeoBackend.Extensions;

public static class CorsOriginParser
{
    private static readonly string[] DefaultOrigins =
    [
        "http://localhost:3000",
        "http://localhost:3003",
        "https://seo.geekatyourspot.com",
        "https://geek-content-creator.geekatyourspot.com",
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

    /// <summary>
    /// True for an origin we own. Every first-party frontend lives on this apex, and enumerating
    /// them one at a time meant each new host silently failed CORS until someone noticed — the
    /// SignalR hub rejected geek-content-creator.geekatyourspot.com this way, with the browser
    /// reporting only "Failed to fetch".
    /// <para>
    /// Scheme is required to be https and the host must be the apex or one of its subdomains, so
    /// this cannot match a lookalike such as <c>geekatyourspot.com.evil.test</c>.
    /// </para>
    /// </summary>
    public static bool IsOwnOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && (uri.Host.Equals("geekatyourspot.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".geekatyourspot.com", StringComparison.OrdinalIgnoreCase));

    private static string NormalizeOrigin(string origin) =>
        origin.EndsWith('/') ? origin[..^1] : origin;
}
