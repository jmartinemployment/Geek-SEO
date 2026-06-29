using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Serp.Models;

namespace SiteAnalyzer2.Services.Pipeline;

public class SerpExternalCompletionService(
    AppDbContext db,
    IAnalysisRunRepository runRepository,
    SerpHtmlImportService serpHtmlImport,
    RunGateService runGate,
    IRunProgressNotifier progressNotifier)
{
    public async Task<SerpClaimStatus> ClaimAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await runRepository.GetByIdAsync(runId, ct);
        if (run is null)
            return SerpClaimStatus.NotFound;

        if (run.Status != RunStatus.Running || run.CurrentStage != PipelineStage.Serp)
            return SerpClaimStatus.NotClaimable;

        if (run.RunGates.Any(g => g.Stage == PipelineStage.Serp))
            return SerpClaimStatus.AlreadyCompleted;

        if (run.SerpClaimedAt.HasValue)
            return SerpClaimStatus.AlreadyClaimed;

        var updated = await db.AnalysisRuns
            .Where(r => r.Id == runId
                && r.SerpClaimedAt == null
                && r.Status == RunStatus.Running
                && r.CurrentStage == PipelineStage.Serp)
            .Where(r => !db.RunGates.Any(g => g.RunId == runId && g.Stage == PipelineStage.Serp))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.SerpClaimedAt, DateTime.UtcNow), ct);

        if (updated == 0)
            return SerpClaimStatus.Conflict;

        await progressNotifier.NotifySerpClaimed(runId, ct);
        return SerpClaimStatus.Success;
    }

    public async Task<SerpWorkerResultStatus> CompleteAsync(
        Guid runId,
        SerpWorkerResultInput input,
        CancellationToken ct = default)
    {
        var run = await runRepository.GetByIdAsync(runId, ct);
        if (run is null)
            return SerpWorkerResultStatus.NotFound;

        if (run.RunGates.Any(g => g.Stage == PipelineStage.Serp))
            return SerpWorkerResultStatus.AlreadyCompleted;

        if (run.Status != RunStatus.Running
            || run.CurrentStage != PipelineStage.Serp
            || !run.SerpClaimedAt.HasValue)
        {
            return SerpWorkerResultStatus.NotClaimable;
        }

        if (!input.Success)
        {
            var message = string.IsNullOrWhiteSpace(input.FailureMessage)
                ? "SERP worker reported failure."
                : input.FailureMessage;
            await runGate.FailStageAsync(run, PipelineStage.Serp, message, ct);
            return SerpWorkerResultStatus.Success;
        }

        if (string.IsNullOrWhiteSpace(input.Html))
        {
            await runGate.FailStageAsync(run, PipelineStage.Serp, "SERP worker did not provide HTML.", ct);
            return SerpWorkerResultStatus.Success;
        }

        await serpHtmlImport.ImportHtmlAsync(run, input.Html, run.Keyword, ct);
        return SerpWorkerResultStatus.Success;
    }
}

public enum SerpClaimStatus
{
    Success,
    NotFound,
    NotClaimable,
    AlreadyCompleted,
    AlreadyClaimed,
    Conflict
}

public enum SerpWorkerResultStatus
{
    Success,
    NotFound,
    NotClaimable,
    AlreadyCompleted
}

public record SerpWorkerResultInput(
    bool Success,
    string? Html,
    string? FailureMessage);
