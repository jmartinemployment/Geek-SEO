using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>Local vs national SERP query outcomes for Step 11 visibility.</summary>
public sealed record SerpLocalQueryStats(
    string Location,
    int Attempted,
    int Succeeded,
    /// <summary>HTTP/provider failures (429, timeouts, etc.).</summary>
    int Failed,
    /// <summary>Successful queries that returned no local pack or places.</summary>
    int Empty,
    string? FirstError);

internal static class SerpValidationMessages
{
    internal const string WarningPrefix = "Local SERP issue:";

    internal static SerpLocalQueryStats? BuildLocalStats(
        string location,
        int attempted,
        int localSuccesses,
        int localApiFailures,
        int localEmpty,
        string? firstLocalError)
    {
        var isLocalMarket = !string.IsNullOrWhiteSpace(location)
            && !location.Equals(PillarDemandEnricher.NationalLocation, StringComparison.OrdinalIgnoreCase);
        if (!isLocalMarket || attempted == 0)
            return null;

        return new SerpLocalQueryStats(location, attempted, localSuccesses, localApiFailures, localEmpty, firstLocalError);
    }

    internal static (string Summary, string? Warning) Build(
        IReadOnlyList<PillarSerpEnrichment> validations,
        IReadOnlyList<NicheCompetitor> competitors,
        bool skipped,
        string? skipReason,
        SerpLocalQueryStats? localStats,
        int demotedCount = 0)
    {
        if (skipped)
        {
            var fail = $"SERP validation skipped — {skipReason ?? "provider unavailable"}.";
            var warn = localStats is { Failed: > 0 } or null && skipReason?.Contains("429", StringComparison.Ordinal) == true
                ? $"{WarningPrefix} {skipReason}. Re-run Step 11 after rate limits clear."
                : null;
            return (fail, warn);
        }

        var baseMsg = demotedCount > 0
            ? $"SERP validation: {validations.Count} pillar(s) checked, {demotedCount} demoted, {competitors.Count} competitor(s) found."
            : $"SERP validation: {validations.Count} pillar(s) checked, {competitors.Count} competitor(s) found.";

        if (localStats is null)
            return (baseMsg, null);

        var localScoped = competitors.Count(c =>
            string.Equals(c.Scope, "local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.Scope, "both", StringComparison.OrdinalIgnoreCase));

        if (localStats.Failed > 0)
        {
            var detail = string.IsNullOrWhiteSpace(localStats.FirstError)
                ? "Provider error"
                : localStats.FirstError;
            var warning =
                $"{WarningPrefix} Local query failed for {localStats.Failed}/{localStats.Attempted} pillars ({localStats.Location}). {detail}. Competitors below may be US-only until you re-run Step 11.";
            return ($"{baseMsg} {warning}", warning);
        }

        if (localStats.Empty > 0)
        {
            var note =
                $"Local SERP: {localStats.Succeeded}/{localStats.Attempted} pillars returned local results for {localStats.Location}; {localStats.Empty} had no local pack (normal for some topics).";
            if (localScoped == 0 && localStats.Succeeded > 0)
            {
                var warning =
                    $"{WarningPrefix} Local SERP returned results for {localStats.Succeeded}/{localStats.Attempted} pillars but no competitors have local scope — re-run Step 11 after deploy, and confirm Business Address + service radius in project settings.";
                return ($"{baseMsg} {note} {warning}", warning);
            }

            return ($"{baseMsg} {note}", null);
        }

        if (localStats.Succeeded == 0)
        {
            var warning =
                $"{WarningPrefix} No local SERP results returned for {localStats.Location} ({localStats.Attempted} pillars queried).";
            return ($"{baseMsg} {warning}", warning);
        }

        if (localScoped == 0)
        {
            var warning = localStats.Succeeded > 0
                ? $"{WarningPrefix} Local SERP returned results for {localStats.Succeeded}/{localStats.Attempted} pillars but none mapped to competitors within your service radius — check Business Address, radius, and that Maps listings include websites."
                : $"{WarningPrefix} Local SERP ran ({localStats.Attempted} pillars) but no competitors have local scope.";
            return ($"{baseMsg} {warning}", warning);
        }

        return ($"{baseMsg} Local SERP ({localStats.Location}): {localScoped} competitor(s) with local presence.", null);
    }

    internal static string? TryExtractWarning(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return null;
        var idx = summary.IndexOf(WarningPrefix, StringComparison.Ordinal);
        if (idx >= 0)
            return summary[idx..].Trim();

        // Surface legacy summaries that failed loudly in text but predate WarningPrefix.
        if (summary.Contains("429", StringComparison.Ordinal)
            || summary.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || summary.Contains("SERP validation skipped", StringComparison.OrdinalIgnoreCase))
            return $"{WarningPrefix} {summary.Trim()}";

        return null;
    }
}
