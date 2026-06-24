using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IAnalysisRunRepository
{
    Task<Result<IReadOnlyList<AnalysisRunSummary>>> ListByProjectAsync(
        Guid projectId, CancellationToken ct = default);

    /// <summary>
    /// Loads the content-writer SERP export for an analysis run (keyword + aggregated SERP items).
    /// </summary>
    Task<Result<ContentWriterSerpExport>> GetContentWriterExportAsync(Guid runId, CancellationToken ct = default);
}
