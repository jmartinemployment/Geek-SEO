using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Repositories;

public class AnalysisRunRepository(AppDbContext db) : IAnalysisRunRepository
{
    public async Task<AnalysisRun?> GetByIdAsync(Guid runId, CancellationToken ct = default)
    {
        return await db.AnalysisRuns
            .Include(r => r.Project)
            .Include(r => r.RunGates)
            .FirstOrDefaultAsync(r => r.Id == runId, ct);
    }

    public async Task SaveAsync(AnalysisRun run, CancellationToken ct = default)
    {
        var existing = await db.AnalysisRuns.AnyAsync(r => r.Id == run.Id, ct);
        if (existing)
            db.AnalysisRuns.Update(run);
        else
            await db.AnalysisRuns.AddAsync(run, ct);

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PendingSerpRun>> ListPendingSerpRunsAsync(CancellationToken ct = default)
    {
        return await db.AnalysisRuns
            .AsNoTracking()
            .Where(r => r.Status == RunStatus.Running && r.CurrentStage == PipelineStage.Serp)
            .Where(r => r.SerpClaimedAt == null)
            .Where(r => !db.RunGates.Any(g => g.RunId == r.Id && g.Stage == PipelineStage.Serp))
            .OrderBy(r => r.CreatedAt)
            .Select(r => new PendingSerpRun(r.Id, r.ProjectId, r.Keyword, r.SerpProviderKey))
            .ToListAsync(ct);
    }
}
