using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheStepRunners;

/// <summary>
/// Merges legacy json step-status maps with authoritative step-log entries and 14→16 slug aliases.
/// </summary>
internal static class NicheStepStatusEnricher
{
    public static void MergeStepLog(IDictionary<string, string> merged, string? stepLogJson)
    {
        foreach (var entry in NicheAnalysisStepLogJson.Parse(stepLogJson))
        {
            if (string.IsNullOrWhiteSpace(entry.Slug) || string.IsNullOrWhiteSpace(entry.Status))
                continue;

            merged[entry.Slug] = PreferStatus(
                merged.TryGetValue(entry.Slug, out var existing) ? existing : null,
                entry.Status);
        }

        ApplyLegacyStructureAliases(merged);
    }

    public static void ApplyLegacyStructureAliases(IDictionary<string, string> merged)
    {
        if (!merged.TryGetValue("site_structure", out var legacy)
            || !string.Equals(legacy, "complete", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var slug in new[] { "site_crawl", "internal_links", "url_patterns" })
        {
            merged.TryGetValue(slug, out var existing);
            merged[slug] = PreferStatus(existing, "complete");
        }
    }

    internal static string PreferStatus(string? existing, string incoming)
    {
        if (string.IsNullOrWhiteSpace(existing))
            return incoming;

        static int Rank(string? status) => status switch
        {
            "complete" or "error" or "skipped" => 3,
            "running" => 2,
            "pending" => 1,
            _ => 0,
        };

        return Rank(incoming) >= Rank(existing) ? incoming : existing;
    }
}
