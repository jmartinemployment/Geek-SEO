namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>NoisePaths — removed per product direction: if heading its valid, no heading is noise. Kept for API compat where URL/path callers still reference it; IsNoise always false.</summary>
internal static class NoisePaths
{
    internal static readonly HashSet<string> H2Noise = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsNoise(string segment) => false;
}
