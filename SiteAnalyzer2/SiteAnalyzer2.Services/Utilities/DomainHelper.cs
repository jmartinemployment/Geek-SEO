namespace SiteAnalyzer2.Services.Utilities;

public static class DomainHelper
{
    private static readonly HashSet<string> DoublePartTlds =
    [
        "co.uk", "com.au", "co.nz", "org.uk", "net.au", "com.br", "co.jp"
    ];

    private static readonly string[] NoiseSubdomainPrefixes =
    [
        "support.", "help.", "docs.", "status.", "community."
    ];

    public static string GetRegistrableDomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return string.Empty;

        host = host.Trim().ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];

        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2)
            return host;

        var lastTwo = $"{parts[^2]}.{parts[^1]}";
        if (parts.Length >= 3 && DoublePartTlds.Contains(lastTwo))
            return $"{parts[^3]}.{lastTwo}";

        return lastTwo;
    }

    public static bool IsNoiseSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        host = host.Trim().ToLowerInvariant();
        return NoiseSubdomainPrefixes.Any(prefix => host.StartsWith(prefix, StringComparison.Ordinal));
    }

    public static bool HostsMatch(string hostA, string hostB)
    {
        if (string.Equals(hostA, hostB, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(
            GetRegistrableDomain(hostA),
            GetRegistrableDomain(hostB),
            StringComparison.OrdinalIgnoreCase);
    }

    public static string GetHostFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return string.Empty;

        return uri.Host;
    }
}
