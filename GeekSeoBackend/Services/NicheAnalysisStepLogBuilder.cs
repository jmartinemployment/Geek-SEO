using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services;

internal static class NicheAnalysisStepLogBuilder
{
    private const int SampleLimit = 20;

    private static readonly IReadOnlyDictionary<string, string> Titles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["schema"] = "Schema.org",
            ["site_urls"] = "Site URLs",
            ["nav"] = "Navigation",
            ["headings"] = "Homepage headings",
            ["merging"] = "Pillar merge",
            ["profile"] = "Niche profile",
            ["local"] = "Local geography",
            ["coverage"] = "Content coverage",
            ["scoring"] = "Authority score",
            ["complete"] = "Complete",
        };

    internal static NicheAnalysisStepLogEntry Entry(
        int stepNumber,
        string slug,
        string summary,
        IReadOnlyDictionary<string, object?> outputs) =>
        new(
            stepNumber,
            slug,
            Titles.TryGetValue(slug, out var title) ? title : slug,
            "complete",
            summary,
            outputs);

    internal static NicheAnalysisStepLogEntry Schema(int step, SchemaOrgData data, string summary) =>
        Entry(step, "schema", summary, new Dictionary<string, object?>
        {
            ["knowsAboutTopics"] = data.KnowsAboutTopics.Take(SampleLimit).ToArray(),
            ["offerCatalogTopics"] = data.OfferCatalogTopics.Take(SampleLimit).ToArray(),
            ["allSchemaTopics"] = data.ServiceNames.Take(SampleLimit).ToArray(),
            ["areaServed"] = data.AreaServed.Take(SampleLimit).ToArray(),
            ["description"] = data.Description ?? string.Empty,
            ["brandName"] = data.BrandName ?? string.Empty,
            ["becomesPillars"] = true,
        });

    internal static NicheAnalysisStepLogEntry SiteUrls(int step, SitemapData data, string summary) =>
        Entry(step, "site_urls", summary, new Dictionary<string, object?>
        {
            ["totalUrls"] = data.TotalUrlsScanned,
            ["pillarCount"] = data.Pillars.Count,
            ["sampleUrls"] = data.SampleUrls.Take(SampleLimit).ToArray(),
        });

    internal static NicheAnalysisStepLogEntry Nav(int step, NavMenuData data, string summary) =>
        Entry(step, "nav", summary, new Dictionary<string, object?>
        {
            ["extractMethod"] = data.ExtractMethod,
            ["pillarCount"] = data.Pillars.Count,
            ["sampleLabels"] = data.Pillars.Select(p => p.Name).Take(SampleLimit).ToArray(),
        });

    internal static NicheAnalysisStepLogEntry Headings(int step, HomepageHeadings data, string summary) =>
        Entry(step, "headings", summary, new Dictionary<string, object?>
        {
            ["title"] = data.Title ?? string.Empty,
            ["headingCount"] = data.Headings.Count,
            ["sampleHeadings"] = data.Headings
                .Take(SampleLimit)
                .Select(h => $"H{h.Level}: {h.Text}")
                .ToArray(),
        });

    internal static NicheAnalysisStepLogEntry Merging(
        int step,
        int candidateCount,
        int mergedCount,
        IReadOnlyList<DiscoveredPillar> merged,
        int fromSchema,
        int fromSitemap,
        int fromNav,
        int fromHeadings,
        IReadOnlyList<DiscoveredPillar> excludedByCap,
        int pillarCap,
        int fromPageContent,
        string fusionVersion,
        IReadOnlyList<string> exclusionReasonsSample,
        string summary) =>
        Entry(step, "merging", summary, new Dictionary<string, object?>
        {
            ["candidateCount"] = candidateCount,
            ["mergedCount"] = mergedCount,
            ["fromSchema"] = fromSchema,
            ["fromSitemap"] = fromSitemap,
            ["fromNav"] = fromNav,
            ["fromHeadings"] = fromHeadings,
            ["fromPageContent"] = fromPageContent,
            ["fusionVersion"] = fusionVersion,
            ["pillarCap"] = pillarCap,
            ["excludedByCapCount"] = excludedByCap.Count,
            ["excludedSampleNames"] = excludedByCap.Select(p => p.Name).Take(SampleLimit).ToArray(),
            ["exclusionReasonsSample"] = exclusionReasonsSample.Take(SampleLimit).ToArray(),
            ["primarySource"] = DescribePrimarySource(merged),
            ["samplePillarNames"] = merged.Select(p => p.Name).Take(SampleLimit).ToArray(),
            ["pillarSources"] = merged
                .Take(SampleLimit)
                .Select(p => $"{p.Name} ({p.Source})")
                .ToArray(),
        });

    private static string DescribePrimarySource(IReadOnlyList<DiscoveredPillar> merged)
    {
        if (merged.Count == 0) return "none";
        var sources = merged.GroupBy(p => p.Source).OrderByDescending(g => g.Count()).ToList();
        if (sources.Count == 1) return sources[0].Key;
        return $"mixed ({string.Join(", ", sources.Select(g => $"{g.Key}:{g.Count()}"))})";
    }

    internal static NicheAnalysisStepLogEntry Profile(
        int step, string primaryNiche, string audienceType, IEnumerable<string> nicheTags, string summary) =>
        Entry(step, "profile", summary, new Dictionary<string, object?>
        {
            ["primaryNiche"] = primaryNiche,
            ["audienceType"] = audienceType,
            ["nicheTags"] = nicheTags.Take(SampleLimit).ToArray(),
        });

    internal static NicheAnalysisStepLogEntry LocalDisabled(int step, string summary) =>
        Entry(step, "local", summary, new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["message"] = summary,
        });

    internal static NicheAnalysisStepLogEntry CoverageDisabled(int step, string summary) =>
        Entry(step, "coverage", summary, new Dictionary<string, object?>
        {
            ["enabled"] = false,
            ["message"] = summary,
        });

    internal static NicheAnalysisStepLogEntry Scoring(
        int step, decimal authorityScore, int covered, int partial, int gap, int pillarCount, string summary) =>
        Entry(step, "scoring", summary, new Dictionary<string, object?>
        {
            ["authorityScore"] = authorityScore,
            ["covered"] = covered,
            ["partial"] = partial,
            ["gap"] = gap,
            ["pillarCount"] = pillarCount,
        });

    internal static NicheAnalysisStepLogEntry Complete(
        int step, DateTimeOffset analyzedAt, DateTimeOffset nextDue, string summary) =>
        Entry(step, "complete", summary, new Dictionary<string, object?>
        {
            ["analyzedAt"] = analyzedAt,
            ["nextAnalysisDue"] = nextDue,
        });
}
