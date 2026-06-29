using SiteAnalyzer2.Domain.Entities;

namespace SiteAnalyzer2.Repositories;

public interface IAnalysisRunRepository
{
    Task<AnalysisRun?> GetByIdAsync(Guid runId, CancellationToken ct = default);
    Task SaveAsync(AnalysisRun run, CancellationToken ct = default);
    Task<IReadOnlyList<PendingSerpRun>> ListPendingSerpRunsAsync(CancellationToken ct = default);
}
