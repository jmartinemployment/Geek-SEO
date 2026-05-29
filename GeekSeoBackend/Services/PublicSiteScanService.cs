using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public sealed partial class PublicSiteScanService(IHttpClientFactory httpClientFactory, ILogger<PublicSiteScanService> logger)
    : IPublicSiteScanService
{
    private static readonly string[] NextStepLabels =
    [
        "Site audit — technical SEO crawl",
        "Topical map — topic clusters for your domain",
        "Competitor analysis — who ranks for your keywords",
    ];

    public async Task<(bool Ok, PublicScanResponse? Result, string? Error)> ScanAsync(string rawUrl, CancellationToken ct)
    {
        if (!TryNormalizePublicUrl(rawUrl, out var normalizedUrl, out var normalizeError))
            return (false, null, normalizeError);

        if (!await IsSafePublicTargetAsync(normalizedUrl, ct))
            return (false, null, "That URL cannot be scanned.");

        var pageClient = httpClientFactory.CreateClient("PublicScanPage");
        var onPage = await FetchOnPageSignalsAsync(pageClient, normalizedUrl, ct);
        if (onPage.Error is not null)
            return (false, null, onPage.Error);

        var psi = await FetchPageSpeedAsync(normalizedUrl, ct);

        return (true, new PublicScanResponse(
            Url: normalizedUrl,
            PerformanceScore: psi.PerformanceScore,
            SeoScore: psi.SeoScore,
            AccessibilityScore: psi.AccessibilityScore,
            Lcp: psi.Lcp,
            Cls: psi.Cls,
            Inp: psi.Inp,
            Title: onPage.Title,
            MetaDescription: onPage.MetaDescription,
            H1: onPage.H1,
            Canonical: onPage.Canonical,
            RobotsTxtFound: onPage.RobotsTxtFound,
            PageSpeedAvailable: psi.Available,
            NextSteps: NextStepLabels), null);
    }

    internal static bool TryNormalizePublicUrl(string raw, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;
        var trimmed = raw.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            error = "Enter a website URL.";
            return false;
        }

        var candidate = trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            error = "Enter a valid website URL (for example, example.com).";
            return false;
        }

        if (uri.Host.Contains('@', StringComparison.Ordinal))
        {
            error = "Enter a website URL, not an email address.";
            return false;
        }

        normalized = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}".TrimEnd('/');
        if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/")
            normalized = $"{uri.Scheme}://{uri.Authority}";

        return true;
    }

    private async Task<bool> IsSafePublicTargetAsync(string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, ct);
            foreach (var address in addresses)
            {
                if (IsBlockedAddress(address))
                    return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DNS lookup failed for public scan host {Host}", host);
            return false;
        }

        return true;
    }

    private static bool IsBlockedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] is >= 16 and <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false,
            };
        }

        return false;
    }

    private async Task<(string? Title, string? MetaDescription, string? H1, string? Canonical, bool? RobotsTxtFound, string? Error)>
        FetchOnPageSignalsAsync(HttpClient client, string url, CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return (null, null, null, null, null, $"Could not reach that site (HTTP {(int)response.StatusCode}).");

            var html = await response.Content.ReadAsStringAsync(ct);
            var robotsTxtFound = await CheckRobotsTxtAsync(client, url, ct);

            return (
                Title: ExtractTitle(html),
                MetaDescription: ExtractMetaDescription(html),
                H1: ExtractFirstH1(html),
                Canonical: ExtractCanonical(html),
                RobotsTxtFound: robotsTxtFound,
                Error: null);
        }
        catch (TaskCanceledException)
        {
            return (null, null, null, null, null, "The site took too long to respond. Try again.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "On-page fetch failed for {Url}", url);
            return (null, null, null, null, null, "Could not fetch that website. Check the URL and try again.");
        }
    }

    private static async Task<bool> CheckRobotsTxtAsync(HttpClient client, string pageUrl, CancellationToken ct)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri))
            return false;

        var robotsUrl = $"{uri.Scheme}://{uri.Host}/robots.txt";
        try
        {
            using var response = await client.GetAsync(robotsUrl, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(int? PerformanceScore, int? SeoScore, int? AccessibilityScore, string? Lcp, string? Cls, string? Inp, bool Available)>
        FetchPageSpeedAsync(string url, CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_PSI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return (null, null, null, null, null, null, false);

        try
        {
            var psiUrl = new UriBuilder("https://www.googleapis.com/pagespeedonline/v5/runPagespeed")
            {
                Query = $"url={Uri.EscapeDataString(url)}&strategy=mobile&category=performance&category=seo&category=accessibility&key={Uri.EscapeDataString(apiKey.Trim())}",
            }.Uri;

            var client = httpClientFactory.CreateClient("PublicScanPsi");
            using var response = await client.GetAsync(psiUrl, ct);
            if (!response.IsSuccessStatusCode)
                return (null, null, null, null, null, null, false);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("lighthouseResult", out var lighthouse))
                return (null, null, null, null, null, null, false);

            var categories = lighthouse.GetProperty("categories");
            var audits = lighthouse.GetProperty("audits");

            return (
                PerformanceScore: ScoreFromCategory(categories, "performance"),
                SeoScore: ScoreFromCategory(categories, "seo"),
                AccessibilityScore: ScoreFromCategory(categories, "accessibility"),
                Lcp: DisplayValue(audits, "largest-contentful-paint"),
                Cls: DisplayValue(audits, "cumulative-layout-shift"),
                Inp: DisplayValue(audits, "interaction-to-next-paint"),
                Available: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PageSpeed Insights request failed for {Url}", url);
            return (null, null, null, null, null, null, false);
        }
    }

    private static int? ScoreFromCategory(JsonElement categories, string key)
    {
        if (!categories.TryGetProperty(key, out var category))
            return null;
        if (!category.TryGetProperty("score", out var score) || score.ValueKind != JsonValueKind.Number)
            return null;
        return (int)Math.Round(score.GetDouble() * 100);
    }

    private static string? DisplayValue(JsonElement audits, string key)
    {
        if (!audits.TryGetProperty(key, out var audit))
            return null;
        if (!audit.TryGetProperty("displayValue", out var display))
            return null;
        return display.GetString();
    }

    private static string? ExtractTitle(string html) =>
        TitleRegex().Match(html).Groups[1].Value.Trim();

    private static string? ExtractMetaDescription(string html)
    {
        var match = MetaDescriptionRegex().Match(html);
        if (match.Success)
            return match.Groups[1].Value.Trim();

        match = MetaDescriptionAltRegex().Match(html);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractFirstH1(string html) =>
        H1Regex().Match(html).Groups[1].Value.Trim();

    private static string? ExtractCanonical(string html) =>
        CanonicalRegex().Match(html).Groups[1].Value.Trim();

    [GeneratedRegex("<title[^>]*>([^<]*)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<meta[^>]+name=[\"']description[\"'][^>]+content=[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex MetaDescriptionRegex();

    [GeneratedRegex("<meta[^>]+content=[\"']([^\"']*)[\"'][^>]+name=[\"']description[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex MetaDescriptionAltRegex();

    [GeneratedRegex("<h1[^>]*>([^<]*)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex H1Regex();

    [GeneratedRegex("<link[^>]+rel=[\"']canonical[\"'][^>]+href=[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex CanonicalRegex();
}
