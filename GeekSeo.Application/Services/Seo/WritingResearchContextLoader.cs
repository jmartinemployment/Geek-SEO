using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class WritingResearchContextLoader(
    IAnalysisRunRepository analysisRuns,
    ISiteAnalyzer2SiteProfileRepository siteProfiles,
    OperatorResearchEnricher operatorResearch)
{
    public async Task<Result<WritingResearchContext>> LoadAsync(
        Guid userId,
        SeoContentDocument document,
        CancellationToken ct = default)
    {
        if (!ResearchBackedWriteGate.IsResearchBacked(document))
            return Result<WritingResearchContext>.Failure(ContentWritingBlockMessage.Default);

        if (document.AnalysisRunId is not Guid runId || runId == Guid.Empty)
            return Result<WritingResearchContext>.Failure("Analysis run is required for research-backed content.");

        var exportResult = await analysisRuns.GetContentWriterExportAsync(runId, ct);
        if (!exportResult.IsSuccess || exportResult.Value is null)
            return Result<WritingResearchContext>.Failure(exportResult.Error ?? "Analysis run not found");

        var export = exportResult.Value;
        export = ManualResearchLaneMerger.Merge(export);

        var gate = ResearchBackedWriteGate.ValidateExport(export);
        if (!gate.IsSuccess)
            return Result<WritingResearchContext>.Failure(gate.Error ?? "Analysis run is not ready for writing.");

        // Resolve site bundle: prefer frozen SiteProfileId on document, fall back to GeekSeoProjectId from sa2.
        Result<ContentWriterSiteBundle> siteBundleResult;
        if (document.SiteProfileId is Guid siteProfileId && siteProfileId != Guid.Empty)
        {
            siteBundleResult = await siteProfiles.GetContentWriterBundleAsync(siteProfileId, ct);
        }
        else if (export.GeekSeoProjectId is Guid geekProjectId && geekProjectId != Guid.Empty)
        {
            siteBundleResult = await siteProfiles.GetContentWriterBundleByGeekSeoProjectIdAsync(geekProjectId, ct);
        }
        else
        {
            siteBundleResult = Result<ContentWriterSiteBundle>.NotFound("No site profile linked to this analysis run.");
        }

        if (!siteBundleResult.IsSuccess || siteBundleResult.Value is null)
            return Result<WritingResearchContext>.Failure(siteBundleResult.Error ?? "Site profile not found");

        var location = string.IsNullOrWhiteSpace(document.TargetLocation)
            ? "United States"
            : document.TargetLocation;

        var context = ContentWriterSerpExportMapper.ToWritingResearchContext(
            export,
            userId,
            location,
            document.TargetKeyword);

        var focus = SiteWritingFocusFromBundlesMapper.Map(
            siteBundleResult.Value,
            export,
            document.TargetKeyword);

        var enriched = await operatorResearch.EnrichContextAsync(
            ApplySiteFocus(context, focus),
            export.ManualResearchLanes,
            ct);

        return Result<WritingResearchContext>.Success(enriched);
    }

    public async Task<Result<ContentWriterSerpExport>> LoadEnrichedExportAsync(
        SeoContentDocument document,
        CancellationToken ct = default)
    {
        if (document.AnalysisRunId is not Guid runId || runId == Guid.Empty)
            return Result<ContentWriterSerpExport>.Failure("Analysis run is required.");

        var exportResult = await analysisRuns.GetContentWriterExportAsync(runId, ct);
        if (!exportResult.IsSuccess || exportResult.Value is null)
            return Result<ContentWriterSerpExport>.Failure(exportResult.Error ?? "Analysis run not found");

        var location = string.IsNullOrWhiteSpace(document.TargetLocation)
            ? "United States"
            : document.TargetLocation;

        var enriched = await operatorResearch.EnrichExportAsync(
            ManualResearchLaneMerger.Merge(exportResult.Value),
            document.TargetKeyword ?? exportResult.Value.Keyword,
            location,
            ct);

        return Result<ContentWriterSerpExport>.Success(enriched);
    }

    public static WritingResearchContext ApplySiteFocus(
        WritingResearchContext context,
        SiteWritingFocus focus) =>
        context with
        {
            SiteFocus = focus,
            BusinessContext = SiteWritingFocusSerializer.ToBusinessContext(focus),
        };
}
