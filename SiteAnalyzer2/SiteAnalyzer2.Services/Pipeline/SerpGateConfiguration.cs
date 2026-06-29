namespace SiteAnalyzer2.Services.Pipeline;

public static class SerpGateConfiguration
{
    /// <summary>
    /// Minimum stored <c>serp_items</c> rows for Serp gate pass. Default <c>0</c> — manual import
    /// always completes the stage; the operator reviews the SERP report before advancing.
    /// Set <c>SERP_GATE_MIN_ITEMS=0</c> to pass even when the parser stored nothing (you judge on the report).
    /// </summary>
    public static int ResolveMinItems()
    {
        var raw = Environment.GetEnvironmentVariable("SERP_GATE_MIN_ITEMS")?.Trim();
        if (string.IsNullOrEmpty(raw))
            raw = Environment.GetEnvironmentVariable("SERP_GATE_MIN_ORGANIC")?.Trim();

        if (string.IsNullOrEmpty(raw))
            return 1;

        if (int.TryParse(raw, out var value) && value >= 0)
            return value;

        throw new InvalidOperationException(
            $"SERP_GATE_MIN_ITEMS must be a non-negative integer; got '{raw}'.");
    }

    [Obsolete("Use ResolveMinItems — Serp gate counts any stored SERP item, not organic only.")]
    public static int ResolveMinOrganicResults() => ResolveMinItems();
}
