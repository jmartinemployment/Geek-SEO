using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Resolves a city/region label for operator local-angle SERP queries from frozen site focus.
/// </summary>
public static class OperatorResearchLocalCity
{
    public static string Resolve(string searchLocation, SiteWritingFocus? focus)
    {
        foreach (var candidate in CollectCandidates(searchLocation, focus))
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate.Trim();
        }

        return string.Empty;
    }

    private static IEnumerable<string> CollectCandidates(string searchLocation, SiteWritingFocus? focus)
    {
        if (focus is not null)
        {
            foreach (var node in focus.GeoAnchorNodes)
            {
                var fromNode = ExtractCity(node);
                if (!string.IsNullOrWhiteSpace(fromNode))
                    yield return fromNode;
            }

            var fromServiceArea = ExtractCity(focus.ServiceAreaDescription);
            if (!string.IsNullOrWhiteSpace(fromServiceArea))
                yield return fromServiceArea;
        }

        var fromSearch = ExtractCity(searchLocation);
        if (!string.IsNullOrWhiteSpace(fromSearch))
            yield return fromSearch;

        if (!IsGenericLocation(searchLocation))
            yield return searchLocation.Trim();
    }

    private static string ExtractCity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();

        // "Delray Beach, FL, US" or "Delray Beach, FL"
        var commaParts = trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (commaParts.Length >= 2
            && commaParts[0].Length >= 3
            && !commaParts[0].Contains("County", StringComparison.OrdinalIgnoreCase))
        {
            return commaParts[0];
        }

        // "within 20 miles of 123 Main St, Delray Beach, FL"
        if (trimmed.Contains(" miles of ", StringComparison.OrdinalIgnoreCase))
        {
            var afterMiles = trimmed.Split(" miles of ", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (afterMiles.Length == 2)
            {
                var cityFromAddress = ExtractCity(afterMiles[1]);
                if (!string.IsNullOrWhiteSpace(cityFromAddress))
                    return cityFromAddress;
            }
        }

        // "Broward County, Palm Beach County, Miami-Dade County" — use first county as regional anchor
        if (trimmed.Contains("County", StringComparison.OrdinalIgnoreCase))
        {
            var county = commaParts.FirstOrDefault(p => p.Contains("County", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(county))
                return county.Trim();
        }

        if (!IsGenericLocation(trimmed) && trimmed.Length <= 48)
            return trimmed;

        return string.Empty;
    }

    private static bool IsGenericLocation(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Equals("United States", StringComparison.OrdinalIgnoreCase)
        || value.Equals("US", StringComparison.OrdinalIgnoreCase);
}
