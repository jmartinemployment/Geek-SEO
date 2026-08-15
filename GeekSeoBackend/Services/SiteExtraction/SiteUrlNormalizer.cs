using GeekSeo.Application.Infrastructure;

namespace GeekSeoBackend.Services.SiteExtraction;

internal static class SiteUrlNormalizer
{
    public static string Normalize(string raw) => SeoSiteUrlNormalizer.Normalize(raw);
}

/// <summary>
/// Fetch-layer URL identity: strip tracking params, drop default ports, treat www/apex and
/// http/https as the same site, keep path case.
/// </summary>
internal static class CrawlUrl
{
    private static readonly string[] TrackingParams =
    [
        "gclid", "fbclid", "msclkid", "mc_cid", "mc_eid",
    ];

    public static string Canonicalize(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var host = uri.Host.ToLowerInvariant();
        var builder = new UriBuilder
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = host,
            Path = uri.AbsolutePath,
            Fragment = string.Empty,
        };

        if ((builder.Scheme == "http" && uri.IsDefaultPort) ||
            (builder.Scheme == "https" && uri.IsDefaultPort))
        {
            builder.Port = -1;
        }
        else if (!uri.IsDefaultPort)
        {
            builder.Port = uri.Port;
        }

        builder.Query = BuildSortedQuery(uri.Query);
        var result = builder.Uri.GetLeftPart(UriPartial.Path);
        if (!string.IsNullOrEmpty(builder.Query))
            result += builder.Query.StartsWith('?') ? builder.Query : "?" + builder.Query;
        return result;
    }

    public static bool IsSameSite(Uri a, Uri b) =>
        string.Equals(HostKey(a), HostKey(b), StringComparison.Ordinal);

    public static bool IsSameSite(string url, string siteUrl)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var a))
            return false;
        if (!Uri.TryCreate(EnsureAbsolute(siteUrl), UriKind.Absolute, out var b))
            return false;
        return IsSameSite(a, b);
    }

    public static string HostKey(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();
        return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
    }

    public static string StripFragment(string url)
    {
        var hash = url.IndexOf('#');
        return hash >= 0 ? url[..hash] : url;
    }

    private static string EnsureAbsolute(string siteUrl) =>
        siteUrl.Contains("://", StringComparison.Ordinal) ? siteUrl : "https://" + siteUrl;

    private static string BuildSortedQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
            return string.Empty;

        var raw = query.StartsWith('?') ? query[1..] : query;
        if (raw.Length == 0)
            return string.Empty;

        var kept = new List<string>();
        foreach (var part in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            var name = eq >= 0 ? part[..eq] : part;
            if (IsTrackingParam(name))
                continue;
            kept.Add(part);
        }

        kept.Sort(StringComparer.Ordinal);
        return kept.Count == 0 ? string.Empty : "?" + string.Join('&', kept);
    }

    private static bool IsTrackingParam(string name)
    {
        if (name.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
            return true;
        foreach (var param in TrackingParams)
        {
            if (name.Equals(param, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
