using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Validates Site Analyzer handoff from a single <c>analysisRunId</c>. Research stays in <c>sa2</c>.
/// </summary>
public sealed class ContentWriterHandoffService(IAnalysisRunRepository analysisRuns)
{
    public async Task<Result<ContentWriterHandoffResult>> ValidateAsync(
        Guid analysisRunId,
        string? articleKeyword = null,
        CancellationToken ct = default)
    {
        if (analysisRunId == Guid.Empty)
            return Result<ContentWriterHandoffResult>.Failure("analysisRunId is required.");

        var exportResult = await analysisRuns.GetContentWriterExportAsync(analysisRunId, ct);
        if (!exportResult.IsSuccess || exportResult.Value is null)
            return Result<ContentWriterHandoffResult>.Failure(exportResult.Error ?? "Analysis run not found");

        var export = exportResult.Value;
        var runGate = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);
        if (!runGate.IsSuccess)
            return Result<ContentWriterHandoffResult>.Failure(runGate.Error ?? "Analysis run is not ready");

        var targetKeyword = string.IsNullOrWhiteSpace(articleKeyword) ? export.Keyword : articleKeyword.Trim();

        return Result<ContentWriterHandoffResult>.Success(new ContentWriterHandoffResult
        {
            GeekSeoProjectId = export.GeekSeoProjectId,
            TargetKeyword = targetKeyword,
            SerpKeyword = export.Keyword,
            AnalysisRunId = analysisRunId,
        });
    }
}

public sealed record ContentWriterHandoffResult
{
    public Guid? GeekSeoProjectId { get; init; }
    public required string TargetKeyword { get; init; }
    public required string SerpKeyword { get; init; }
    public required Guid AnalysisRunId { get; init; }
}
