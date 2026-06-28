using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Mapping;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Fetches SA2 site + keyword bundles once at create/attach and produces frozen writer inputs.
/// </summary>
public sealed class ContentWriterHandoffService(
    IAnalysisRunRepository analysisRuns,
    ISiteAnalyzer2SiteProfileRepository siteProfiles)
{
    public async Task<Result<ContentWriterHandoffFreeze>> FreezeAsync(
        Guid analysisRunId,
        Guid siteProfileId,
        string articleKeyword,
        string searchLocation,
        CancellationToken ct = default)
    {
        if (siteProfileId == Guid.Empty)
            return Result<ContentWriterHandoffFreeze>.Failure("site_profile is required for research-backed content.");

        var exportTask = analysisRuns.GetContentWriterExportAsync(analysisRunId, ct);
        var siteBundleTask = siteProfiles.GetContentWriterBundleAsync(siteProfileId, ct);
        await Task.WhenAll(exportTask, siteBundleTask);

        var exportResult = exportTask.Result;
        if (!exportResult.IsSuccess || exportResult.Value is null)
            return Result<ContentWriterHandoffFreeze>.Failure(exportResult.Error ?? "Analysis run not found");

        var export = exportResult.Value;
        var runGate = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);
        if (!runGate.IsSuccess)
            return Result<ContentWriterHandoffFreeze>.Failure(runGate.Error ?? "Analysis run is not ready");

        var siteBundleResult = siteBundleTask.Result;
        if (!siteBundleResult.IsSuccess || siteBundleResult.Value is null)
            return Result<ContentWriterHandoffFreeze>.Failure(siteBundleResult.Error ?? "Site profile not found");

        var siteBundle = siteBundleResult.Value;
        if (siteBundle.GeekSeoProjectId is not Guid geekSeoProjectId || geekSeoProjectId == Guid.Empty)
            return Result<ContentWriterHandoffFreeze>.Failure("Site profile is not linked to a Geek-SEO project.");

        var targetKeyword = string.IsNullOrWhiteSpace(articleKeyword) ? export.Keyword : articleKeyword.Trim();
        var focus = SiteWritingFocusFromBundlesMapper.Map(siteBundle, export, targetKeyword);

        return Result<ContentWriterHandoffFreeze>.Success(new ContentWriterHandoffFreeze
        {
            GeekSeoProjectId = geekSeoProjectId,
            TargetKeyword = targetKeyword,
            SerpKeyword = export.Keyword,
            SiteFocusJson = SiteWritingFocusSerializer.Serialize(focus),
            SiteFocusCapturedAt = focus.CapturedAt,
            KeywordBundleJson = ContentWriterKeywordBundleSerializer.Serialize(export),
            KeywordBundleCapturedAt = export.CapturedAt != default ? export.CapturedAt : focus.CapturedAt,
            SiteProfileId = siteProfileId,
            AnalysisRunId = analysisRunId,
        });
    }
}

public sealed record ContentWriterHandoffFreeze
{
    public required Guid GeekSeoProjectId { get; init; }
    public required string TargetKeyword { get; init; }
    public required string SerpKeyword { get; init; }
    public required string SiteFocusJson { get; init; }
    public required DateTimeOffset SiteFocusCapturedAt { get; init; }
    public required string KeywordBundleJson { get; init; }
    public required DateTimeOffset KeywordBundleCapturedAt { get; init; }
    public required Guid SiteProfileId { get; init; }
    public required Guid AnalysisRunId { get; init; }
}
