using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public static class ResearchBackedWriteGate
{
    public static bool IsResearchBacked(SeoContentDocument document) =>
        document.AnalysisRunId is not null;

    public static bool IsManualResearchMode(ContentWriterSerpExport export) =>
        string.Equals(export.ResearchMode, ResearchModes.Manual, StringComparison.OrdinalIgnoreCase);

    public static Result ValidateExport(ContentWriterSerpExport export) =>
        IsManualResearchMode(export)
            ? ValidateManualResearchExport(export)
            : ValidateAnalysisRunExport(export);

    /// <summary>
    /// Manual five-lane research — keyword + required supplemental lanes; no competitor crawl.
    /// </summary>
    public static Result ValidateManualResearchExport(ContentWriterSerpExport export)
    {
        if (string.Equals(export.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            return Result.Failure("Analysis run failed — SERP data is not available for Content Writing.");

        var organicCount = export.Serp.Count(i =>
            string.Equals(i.Type, "organic", StringComparison.OrdinalIgnoreCase));

        if (organicCount == 0)
            return Result.Failure("Manual research requires a keyword lane with at least one organic result.");

        var requiredLanes = ResolveRequiredSupplementalLanes(export.TopicSlug);
        foreach (var lane in requiredLanes)
        {
            var imported = export.ManualResearchLanes.FirstOrDefault(l =>
                string.Equals(l.Lane, lane, StringComparison.OrdinalIgnoreCase));

            if (imported is null || imported.OrganicCount == 0)
            {
                return Result.Failure(
                    $"Manual research requires imported '{lane}' lane with at least one usable result.");
            }
        }

        return Result.Success();
    }

    private static IReadOnlyList<string> ResolveRequiredSupplementalLanes(string? topicSlug)
    {
        if (string.Equals(topicSlug, "customer-journey", StringComparison.OrdinalIgnoreCase))
            return [SerpResearchLanes.Gov, SerpResearchLanes.Wiki];

        return [];
    }

    /// <summary>
    /// Aligns with Site Analyzer <c>research-focus</c> gates — thin packs fail at document create.
    /// </summary>
    public static Result ValidateAnalysisRunExport(ContentWriterSerpExport export)
    {
        if (string.Equals(export.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            return Result.Failure("Analysis run failed — SERP data is not available for Content Writing.");

        var organicCount = export.Serp.Count(i =>
            string.Equals(i.Type, "organic", StringComparison.OrdinalIgnoreCase));

        if (organicCount == 0)
            return Result.Failure("Analysis run has no organic SERP results yet.");

        if (export.SourceHeadings.Count == 0)
            return Result.Failure(
                "Analysis run has no target page headings yet — complete target crawl in Site Analyzer.");

        if (export.Competitors.Count == 0
            || export.Competitors.All(c => c.Headings.Count == 0))
        {
            return Result.Failure(
                "Analysis run has no competitor crawl headings yet — run competitor crawl in Site Analyzer.");
        }

        if (export.GapTopics.Count == 0)
            return Result.Failure(
                "Analysis run has no gap topics yet — wait for research assembly in Site Analyzer.");

        return Result.Success();
    }

    public static Result ForbidLiveSerp(string operation) =>
        Result.Failure($"Live SERP is forbidden for research-backed documents during {operation}.");
}
