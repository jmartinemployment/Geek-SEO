using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Integrations;
namespace SiteAnalyzer2.Services.CompetitorCrawl;

public sealed class CompetitorCrawlJobService(
    IServiceScopeFactory scopeFactory,
    CompetitorCrawlProgressPublisher progressPublisher)
{
    private static readonly TimeSpan StaleRunningAfter = TimeSpan.FromMinutes(20);

    public async Task<CompetitorCrawlJobState> GetStateAsync(AppDbContext db, Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.AsNoTracking()
            .Where(r => r.Id == runId)
            .Select(r => new
            {
                r.CompetitorCrawlStatus,
                r.CompetitorCrawlMessage,
                r.CompetitorCrawlStartedAt,
                r.CompetitorCrawlFinishedAt,
            })
            .FirstOrDefaultAsync(ct);

        if (run is null)
            return CompetitorCrawlJobState.Idle;

        if (string.Equals(run.CompetitorCrawlStatus, CompetitorCrawlStatuses.Running, StringComparison.OrdinalIgnoreCase)
            && run.CompetitorCrawlStartedAt is DateTime started
            && DateTime.UtcNow - started > StaleRunningAfter)
        {
            return new CompetitorCrawlJobState
            {
                Status = CompetitorCrawlJobStatus.Failed,
                Message = "Competitor crawl timed out. Run it again.",
                StartedAt = run.CompetitorCrawlStartedAt,
                FinishedAt = DateTimeOffset.UtcNow,
            };
        }

        return new CompetitorCrawlJobState
        {
            Status = MapStatus(run.CompetitorCrawlStatus),
            Message = run.CompetitorCrawlMessage,
            StartedAt = run.CompetitorCrawlStartedAt,
            FinishedAt = run.CompetitorCrawlFinishedAt,
        };
    }

    public async Task<bool> TryStartAsync(AppDbContext db, Guid runId, CancellationToken ct = default)
    {
        var state = await GetStateAsync(db, runId, ct);
        if (state.Status == CompetitorCrawlJobStatus.Running)
            return false;

        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return false;

        var startedAt = DateTime.UtcNow;
        run.CompetitorCrawlStatus = CompetitorCrawlStatuses.Running;
        run.CompetitorCrawlMessage = null;
        run.CompetitorCrawlStartedAt = startedAt;
        run.CompetitorCrawlFinishedAt = null;
        await db.SaveChangesAsync(ct);

        progressPublisher.Publish(new CompetitorCrawlProgressEvent(
            runId,
            CompetitorCrawlStatuses.Running,
            CompetitorSaved: false,
            TotalPages: 0,
            DomainCount: 0,
            Message: "Competitor crawl started.",
            QualityWarnings: []));

        _ = ExecuteAsync(runId, startedAt);
        return true;
    }

    private async Task ExecuteAsync(Guid runId, DateTime startedAt)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var workflow = scope.ServiceProvider.GetRequiredService<KeywordWorkflowService>();
            var result = await workflow.RunCompetitorCrawlAsync(runId);

            var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
            if (run is null)
                return;

            var pageCount = await db.CompetitorPages.CountAsync(p => p.RunId == runId);
            run.CompetitorCrawlFinishedAt = DateTime.UtcNow;

            if (result.CompetitorSaved)
            {
                run.CompetitorCrawlStatus = CompetitorCrawlStatuses.Complete;
                run.CompetitorCrawlMessage = result.Message;
                run.Status = RunStatus.ResearchReady;
            }
            else if (pageCount > 0)
            {
                run.CompetitorCrawlStatus = CompetitorCrawlStatuses.PagesSaved;
                run.CompetitorCrawlMessage = BuildFailureMessage(result);
                run.Status = RunStatus.ResearchFailed;
            }
            else
            {
                run.CompetitorCrawlStatus = CompetitorCrawlStatuses.Failed;
                run.CompetitorCrawlMessage = BuildFailureMessage(result);
            }

            await db.SaveChangesAsync();
            PublishTerminalProgress(runId, result, pageCount);
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId);
                if (run is null)
                    return;

                var pageCount = await db.CompetitorPages.CountAsync(p => p.RunId == runId);
                run.CompetitorCrawlFinishedAt = DateTime.UtcNow;
                if (pageCount > 0)
                {
                    run.CompetitorCrawlStatus = CompetitorCrawlStatuses.PagesSaved;
                    run.CompetitorCrawlMessage = ex.Message;
                    run.Status = RunStatus.ResearchFailed;
                }
                else
                {
                    run.CompetitorCrawlStatus = CompetitorCrawlStatuses.Failed;
                    run.CompetitorCrawlMessage = ex.Message;
                }

                await db.SaveChangesAsync();
                progressPublisher.Publish(new CompetitorCrawlProgressEvent(
                    runId,
                    run.CompetitorCrawlStatus,
                    CompetitorSaved: false,
                    TotalPages: pageCount,
                    DomainCount: 0,
                    Message: ex.Message,
                    QualityWarnings: []));
            }
            catch
            {
                // best-effort status write
            }
        }
    }

    private void PublishTerminalProgress(Guid runId, CompetitorCrawlWorkflowResultDto result, int pageCount)
    {
        var saved = result.CompetitorSaved;
        var status = saved
            ? CompetitorCrawlStatuses.Complete
            : pageCount > 0
                ? CompetitorCrawlStatuses.PagesSaved
                : CompetitorCrawlStatuses.Failed;

        progressPublisher.Publish(new CompetitorCrawlProgressEvent(
            runId,
            status,
            saved,
            result.TotalPages,
            result.DomainCount,
            saved ? result.Message : BuildFailureMessage(result),
            result.QualityWarnings));
    }

    private static string BuildFailureMessage(CompetitorCrawlWorkflowResultDto result)
    {
        if (!string.IsNullOrWhiteSpace(result.Message))
            return result.Message.Trim();

        if (result.QualityWarnings.Count > 0)
            return string.Join(" ", result.QualityWarnings);

        return "Competitor crawl data was not saved.";
    }

    private static CompetitorCrawlJobStatus MapStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            CompetitorCrawlStatuses.Running => CompetitorCrawlJobStatus.Running,
            CompetitorCrawlStatuses.Complete => CompetitorCrawlJobStatus.Complete,
            CompetitorCrawlStatuses.PagesSaved => CompetitorCrawlJobStatus.PagesSaved,
            CompetitorCrawlStatuses.Failed => CompetitorCrawlJobStatus.Failed,
            _ => CompetitorCrawlJobStatus.Idle,
        };
}

public enum CompetitorCrawlJobStatus
{
    Idle,
    Running,
    PagesSaved,
    Complete,
    Failed,
}

public sealed class CompetitorCrawlJobState
{
    public static CompetitorCrawlJobState Idle { get; } = new() { Status = CompetitorCrawlJobStatus.Idle };

    public CompetitorCrawlJobStatus Status { get; init; } = CompetitorCrawlJobStatus.Idle;
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public string? Message { get; init; }
}
