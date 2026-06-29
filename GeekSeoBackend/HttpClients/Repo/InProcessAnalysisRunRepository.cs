using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Infrastructure;
using SiteAnalyzer2.Services.Integrations;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class InProcessAnalysisRunRepository(ContentWriterExportService exportService) : IAnalysisRunRepository
{
    public async Task<Result<IReadOnlyList<AnalysisRunSummary>>> ListByProjectAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var runs = await exportService.ListByProjectAsync(projectId, ct);
        return Result<IReadOnlyList<AnalysisRunSummary>>.Success(
            runs.Select(SiteAnalyzer2ModelMapper.ToSummary).ToList());
    }

    public async Task<Result<ContentWriterSerpExport>> GetContentWriterExportAsync(
        Guid runId, CancellationToken ct = default)
    {
        var export = await exportService.GetExportAsync(runId, ct);
        return export is null
            ? Result<ContentWriterSerpExport>.NotFound("Analysis run not found")
            : Result<ContentWriterSerpExport>.Success(SiteAnalyzer2ModelMapper.ToExport(export));
    }
}
