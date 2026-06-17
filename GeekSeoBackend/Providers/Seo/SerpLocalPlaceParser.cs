using System.Text.Json;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>Extract competitor website domains from Google local pack / Maps places JSON.</summary>
internal static class SerpLocalPlaceParser
{
    internal static IReadOnlyList<string> FromSerperRoot(JsonElement root) =>
        PlacesFromSerperRoot(root).Select(p => p.Domain).ToList();

    internal static IReadOnlyList<string> FromSerpApiRoot(JsonElement root) =>
        PlacesFromSerpApiRoot(root).Select(p => p.Domain).ToList();

    internal static IReadOnlyList<SerpLocalPlace> PlacesFromSerperRoot(JsonElement root)
    {
        if (!root.TryGetProperty("places", out var places) || places.ValueKind != JsonValueKind.Array)
            return [];

        return PlacesFromSerperItems(places);
    }

    internal static IReadOnlyList<SerpLocalPlace> PlacesFromSerpApiRoot(JsonElement root)
    {
        if (!root.TryGetProperty("local_results", out var local))
            return [];

        if (local.ValueKind == JsonValueKind.Array)
            return PlacesFromSerpApiItems(local);

        if (local.TryGetProperty("places", out var places) && places.ValueKind == JsonValueKind.Array)
            return PlacesFromSerpApiItems(places);

        return [];
    }

    private static List<SerpLocalPlace> PlacesFromSerperItems(JsonElement places)
    {
        var results = new List<SerpLocalPlace>();
        foreach (var item in places.EnumerateArray())
        {
            var website = item.TryGetProperty("website", out var w) ? w.GetString() : null;
            var domain = DomainFromUrl(website);
            if (string.IsNullOrWhiteSpace(domain))
                continue;

            if (results.Any(p => string.Equals(p.Domain, domain, StringComparison.OrdinalIgnoreCase)))
                continue;

            var (lat, lon) = ReadCoordinates(item);
            results.Add(new SerpLocalPlace(domain, lat, lon));
        }

        return results;
    }

    private static List<SerpLocalPlace> PlacesFromSerpApiItems(JsonElement places)
    {
        var results = new List<SerpLocalPlace>();
        foreach (var item in places.EnumerateArray())
        {
            string? website = null;
            if (item.TryGetProperty("links", out var links) && links.ValueKind == JsonValueKind.Object)
                website = links.TryGetProperty("website", out var w) ? w.GetString() : null;

            website ??= item.TryGetProperty("website", out var site) ? site.GetString() : null;

            var domain = DomainFromUrl(website);
            if (string.IsNullOrWhiteSpace(domain))
                continue;

            if (results.Any(p => string.Equals(p.Domain, domain, StringComparison.OrdinalIgnoreCase)))
                continue;

            var (lat, lon) = ReadCoordinates(item);
            results.Add(new SerpLocalPlace(domain, lat, lon));
        }

        return results;
    }

    private static (double? Lat, double? Lon) ReadCoordinates(JsonElement item)
    {
        if (item.TryGetProperty("gps_coordinates", out var gps) && gps.ValueKind == JsonValueKind.Object)
        {
            var lat = ReadDouble(gps, "latitude") ?? ReadDouble(gps, "lat");
            var lon = ReadDouble(gps, "longitude") ?? ReadDouble(gps, "lng");
            if (lat.HasValue && lon.HasValue)
                return (lat, lon);
        }

        var directLat = ReadDouble(item, "latitude") ?? ReadDouble(item, "lat");
        var directLon = ReadDouble(item, "longitude") ?? ReadDouble(item, "lng");
        return (directLat, directLon);
    }

    private static double? ReadDouble(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String when double.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null,
        };
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
