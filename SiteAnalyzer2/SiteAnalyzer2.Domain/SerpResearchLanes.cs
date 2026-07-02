namespace SiteAnalyzer2.Domain;

/// <summary>Manual Google SERP research lane identifiers stored on <c>serp_items.research_lane</c>.</summary>
public static class SerpResearchLanes
{
    public const string Keyword = "keyword";
    public const string Edu = "edu";
    public const string Gov = "gov";
    public const string Local = "local";
    public const string Wiki = "wiki";
    public const string Paa = "paa";

    public static readonly IReadOnlySet<string> Supplemental =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Edu, Gov, Local, Paa, Wiki };

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Keyword, Edu, Gov, Local, Paa, Wiki };

    public static bool IsSupplemental(string? lane) =>
        !string.IsNullOrWhiteSpace(lane) && Supplemental.Contains(lane);

    public static string Normalize(string? lane)
    {
        if (string.IsNullOrWhiteSpace(lane) || string.Equals(lane, Keyword, StringComparison.OrdinalIgnoreCase))
            return Keyword;

        var trimmed = lane.Trim().ToLowerInvariant();
        return All.Contains(trimmed) ? trimmed : Keyword;
    }

    public static string? ToStorageValue(string normalizedLane) =>
        string.Equals(normalizedLane, Keyword, StringComparison.OrdinalIgnoreCase) ? null : normalizedLane;

    public static string DisplayLabel(string lane) => lane.ToLowerInvariant() switch
    {
        Keyword => "Keyword SERP",
        Edu => "Research (.edu)",
        Gov => "Government",
        Local => "Local",
        Wiki => "Wikipedia",
        Paa => "People Also Ask",
        _ => lane,
    };
}
