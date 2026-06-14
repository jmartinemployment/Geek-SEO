using System.Text.Json;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.NicheExtraction;

namespace GeekSeoBackend.Services.NicheStepRunners;

internal static class NicheStepArtifactStore
{
    private const string ArtifactJsonKey = "_artifactJson";
    private const string ArtifactTypeKey = "_artifactType";
    private const string ArtifactVersionKey = "_artifactVersion";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal sealed record SiteStructureArtifact(
        SiteCrawlData Crawl,
        InternalLinkData InternalLinks,
        UrlPatternData UrlPatterns,
        IReadOnlyList<string> CrawledUrls);

    public static NicheAnalysisStepLogEntry WithArtifact<TArtifact>(
        NicheAnalysisStepLogEntry entry,
        string artifactType,
        TArtifact artifact)
    {
        var persisted = StripHtmlForPersistence(artifact);
        var outputs = new Dictionary<string, object?>(entry.Outputs, StringComparer.OrdinalIgnoreCase)
        {
            [ArtifactTypeKey] = artifactType,
            [ArtifactVersionKey] = 1,
            [ArtifactJsonKey] = JsonSerializer.Serialize(persisted, Json),
        };

        return entry with { Outputs = outputs };
    }

    /// <summary>
    /// Crawl HTML is kept in memory for extractors but must not be written into
    /// <c>analysis_step_log</c> — SPA shells can be megabytes per page and explode Supabase egress.
    /// </summary>
    private static object StripHtmlForPersistence<TArtifact>(TArtifact artifact) =>
        artifact switch
        {
            SiteStructureArtifact ssa => ssa with { Crawl = StripHtml(ssa.Crawl) },
            SiteCrawlData scd => StripHtml(scd),
            _ => artifact!,
        };

    private static SiteCrawlData StripHtml(SiteCrawlData crawl)
    {
        if (crawl.Pages.Count == 0)
            return crawl;

        var stripped = crawl.Pages
            .Select(p => p with { Html = string.Empty })
            .ToList();
        return crawl with { Pages = stripped };
    }

    public static TArtifact GetRequiredArtifact<TArtifact>(
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        string slug,
        string artifactType)
    {
        var artifact = TryGetArtifact<TArtifact>(steps, slug, artifactType);
        if (artifact is null)
            throw new InvalidOperationException(
                $"Required artifact '{artifactType}' for step '{slug}' is not available.");

        return artifact;
    }

    public static TArtifact? TryGetArtifact<TArtifact>(
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        string slug,
        string artifactType)
    {
        var step = steps.FirstOrDefault(s => s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        if (step is null)
            return default;

        if (!step.Outputs.TryGetValue(ArtifactTypeKey, out var rawType)
            || !string.Equals(rawType?.ToString(), artifactType, StringComparison.OrdinalIgnoreCase))
            return default;

        if (!step.Outputs.TryGetValue(ArtifactJsonKey, out var rawJson))
            return default;

        var json = rawJson?.ToString();
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<TArtifact>(json, Json);
    }

    public static IReadOnlyList<NicheAnalysisStepLogEntry> ParseSteps(string? stepLogJson) =>
        NicheAnalysisStepLogJson.Parse(stepLogJson);
}
