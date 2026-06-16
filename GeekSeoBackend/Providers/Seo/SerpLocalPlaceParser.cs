using System.Text.Json;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>Extract competitor website domains from Google local pack / Maps places JSON.</summary>
internal static class SerpLocalPlaceParser
{
    internal static IReadOnlyList<string> FromSerperRoot(JsonElement root)
    {
        if (!root.TryGetProperty("places", out var places) || places.ValueKind != JsonValueKind.Array)
            return [];

        return DomainsFromPlaceItems(places);
    }

    internal static IReadOnlyList<string> FromSerpApiRoot(JsonElement root)
    {
        if (!root.TryGetProperty("local_results", out var local))
            return [];

        if (local.ValueKind == JsonValueKind.Array)
            return DomainsFromSerpApiPlaceItems(local);

        if (local.TryGetProperty("places", out var places) && places.ValueKind == JsonValueKind.Array)
            return DomainsFromSerpApiPlaceItems(places);

        return [];
    }

    private static List<string> DomainsFromPlaceItems(JsonElement places)
    {
        var domains = new List<string>();
        foreach (var item in places.EnumerateArray())
        {
            var website = item.TryGetProperty("website", out var w) ? w.GetString() : null;
            AddDomain(domains, website);
        }

        return domains;
    }

    private static List<string> DomainsFromSerpApiPlaceItems(JsonElement places)
    {
        var domains = new List<string>();
        foreach (var item in places.EnumerateArray())
        {
            if (item.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Object)
            {
                var website = links.TryGetProperty("website", out var w) ? w.GetString() : null;
                AddDomain(domains, website);
            }

            var direct = item.TryGetProperty("website", out var site) ? site.GetString() : null;
            AddDomain(domains, direct);
        }

        return domains;
    }

    private static void AddDomain(List<string> domains, string? urlOrDomain)
    {
        var domain = DomainFromUrl(urlOrDomain);
        if (string.IsNullOrWhiteSpace(domain))
            return;

        if (domains.Contains(domain, StringComparer.OrdinalIgnoreCase))
            return;

        domains.Add(domain);
    }

    internal static string? DomainFromUrl(string? urlOrDomain)
    {
        if (string.IsNullOrWhiteSpace(urlOrDomain))
            return null;

        var trimmed = urlOrDomain.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute))
            return StripWww(absolute.Host);

        if (Uri.TryCreate($"https://{trimmed}", UriKind.Absolute, out var withScheme))
            return StripWww(withScheme.Host);

        return StripWww(trimmed);
    }

    private static string StripWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? host[4..].ToLowerInvariant()
            : host.ToLowerInvariant();
}
