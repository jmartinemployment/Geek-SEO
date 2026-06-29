namespace SiteAnalyzer2.Services.Pipeline;

public static class SerpExecutionConfiguration
{
    /// <summary>
    /// manual — run waits at Serp until HTML is imported (production default).
    /// inline — Api runs fixture/google provider during POST /runs (local dev/tests).
    /// external — deprecated worker queue.
    /// </summary>
    public static string Mode =>
        Environment.GetEnvironmentVariable("SERP_EXECUTION")?.Trim().ToLowerInvariant() ?? "manual";

    public static bool IsExternal => Mode == "external";
    public static bool IsInline => Mode == "inline";
    public static bool IsManual => !IsExternal && !IsInline;
}
