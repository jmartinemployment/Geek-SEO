using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;

namespace GeekSeoBackend.Services;

public sealed class UrlResearchJobProcessor(
    IUrlResearchRepository repo,
    IUrlResearchAnalyzeRunner analyze,
    IUrlResearchProgressNotifier progress,
    WorkerUserContext workerUser,
    ILogger<UrlResearchJobProcessor> logger)
{
    private static readonly TimeSpan StaleRunningAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(15);
    private const int BatchSize = 3;

    public async Task ProcessBatchAsync(CancellationToken ct = default)
    {
        var stale = await repo.FailStaleRunningAsync(StaleRunningAge, ct);
        if (stale.IsSuccess && stale.Value > 0)
            logger.LogWarning("Marked {Count} stale url research job(s) as failed", stale.Value);

        var queued = await repo.ListQueuedAsync(BatchSize, ct);
        if (!queued.IsSuccess || queued.Value is null || queued.Value.Count == 0)
            return;

        foreach (var job in queued.Value)
        {
            try
            {
                workerUser.UserId = job.UserId;

                var claimed = await repo.TryClaimRunningAsync(job.UrlResearchId, ct);
                if (!claimed.IsSuccess)
                {
                    logger.LogWarning(
                        "Could not claim url research {Id}: {Error}",
                        job.UrlResearchId,
                        claimed.Error);
                    continue;
                }

                if (!claimed.Value)
                {
                    logger.LogDebug("Url research {Id} already claimed", job.UrlResearchId);
                    continue;
                }

                using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                jobCts.CancelAfter(JobTimeout);

                await progress.PushAsync(
                    job.UrlResearchId,
                    job.ProjectId,
                    job.UserId,
                    "running",
                    "Analyzing page and SERP…",
                    ct: jobCts.Token);

                var fullWrite = await analyze.BuildFullWriteAsync(
                    job.UserId,
                    job.ProjectId,
                    job.SourceUrl,
                    jobCts.Token);

                if (!fullWrite.IsSuccess || fullWrite.Value is null)
                {
                    var failMessage = fullWrite.Error ?? "SERP research failed";
                    await repo.UpdateStatusAsync(
                        job.UrlResearchId,
                        new UrlResearchStatusPatch
                        {
                            Status = "failed",
                            ErrorMessage = failMessage,
                        },
                        jobCts.Token);
                    await progress.PushAsync(
                        job.UrlResearchId,
                        job.ProjectId,
                        job.UserId,
                        "failed",
                        errorMessage: failMessage,
                        ct: CancellationToken.None);
                    logger.LogWarning(
                        "Url research {Id} failed: {Error}",
                        job.UrlResearchId,
                        fullWrite.Error);
                    continue;
                }

                var persisted = await repo.PersistFullAsync(
                    job.UrlResearchId,
                    fullWrite.Value,
                    jobCts.Token);

                if (!persisted.IsSuccess)
                {
                    var failMessage = persisted.Error ?? "Failed to persist page research";
                    await repo.UpdateStatusAsync(
                        job.UrlResearchId,
                        new UrlResearchStatusPatch
                        {
                            Status = "failed",
                            ErrorMessage = failMessage,
                        },
                        CancellationToken.None);
                    await progress.PushAsync(
                        job.UrlResearchId,
                        job.ProjectId,
                        job.UserId,
                        "failed",
                        errorMessage: failMessage,
                        ct: CancellationToken.None);
                    logger.LogError(
                        "Url research {Id} persist failed: {Error}",
                        job.UrlResearchId,
                        persisted.Error);
                    continue;
                }

                await progress.PushAsync(
                    job.UrlResearchId,
                    job.ProjectId,
                    job.UserId,
                    "completed",
                    "Page research complete",
                    ct: CancellationToken.None);

                logger.LogInformation("Url research completed for {Id}", job.UrlResearchId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Url research failed for {Id}", job.UrlResearchId);
                try
                {
                    await repo.UpdateStatusAsync(
                        job.UrlResearchId,
                        new UrlResearchStatusPatch
                        {
                            Status = "failed",
                            ErrorMessage = ex.Message,
                        },
                        CancellationToken.None);
                    await progress.PushAsync(
                        job.UrlResearchId,
                        job.ProjectId,
                        job.UserId,
                        "failed",
                        errorMessage: ex.Message,
                        ct: CancellationToken.None);
                }
                catch (Exception patchEx)
                {
                    logger.LogError(patchEx, "Failed to mark url research {Id} as failed", job.UrlResearchId);
                }
            }
        }
    }
}
