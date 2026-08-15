namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Conservative abort list: ads, analytics, and beacons only. Images, fonts, and media stay —
/// blocking them shifts layout and corrupts <c>data-gsv</c>.
/// </summary>
internal static class TrackerBlocklist
{
    private static readonly string[] HostSuffixes =
    [
        "doubleclick.net",
        "googlesyndication.com",
        "googleadservices.com",
        "googletagmanager.com",
        "google-analytics.com",
        "scorecardresearch.com",
        "quantserve.com",
        "adnxs.com",
        "criteo.com",
        "hotjar.com",
        "hotjar.io",
        "mixpanel.com",
        "segment.io",
        "segment.com",
        "adsystem.com",
    ];

    public static bool ShouldAbort(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();
        foreach (var suffix in HostSuffixes)
        {
            if (host == suffix || host.EndsWith("." + suffix, StringComparison.Ordinal))
                return true;
        }

        // Facebook pixel / beacon only — not facebook.com content embeds.
        if (host is "connect.facebook.net" or "www.facebook.com" or "facebook.com")
        {
            var path = uri.AbsolutePath;
            if (path.StartsWith("/tr", StringComparison.OrdinalIgnoreCase)
                || path.Contains("fbevents", StringComparison.OrdinalIgnoreCase))
                return true;
            if (host == "connect.facebook.net")
                return true;
        }

        return false;
    }
}
