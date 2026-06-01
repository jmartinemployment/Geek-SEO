namespace GeekSeoBackend.Services;

internal static class CannibalizationPageNormalizer
{
    internal static string Normalize(string page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return string.Empty;

        var trimmed = page.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed.ToLowerInvariant();

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];

        var path = uri.AbsolutePath;
        if (path.Length > 1 && path.EndsWith('/'))
            path = path[..^1];

        var builder = new UriBuilder(uri.Scheme.ToLowerInvariant(), host)
        {
            Path = string.IsNullOrEmpty(path) ? "/" : path,
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri.ToString().TrimEnd('/');
    }
}
