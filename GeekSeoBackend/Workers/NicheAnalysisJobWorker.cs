using GeekSeo.Application.Interfaces;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Jobs;

namespace GeekSeoBackend.Workers;

/// <summary>
/// Picks up niche_profiles with status <c>queued</c> and runs analysis.
/// Replaces fire-and-forget Task.Run on the HTTP request (unreliable on serverless hosts).
/// </summary>
public sealed class NicheAnalysisJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    ILogger<NicheAnalysisJobWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StaleProcessingAge = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(8);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueuedAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "NicheAnalysisJobWorker iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessQueuedAsync(CancellationToken ct)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogDebug("WORKER_SERVICE_USER_ID not set — niche analysis queue idle");
            return;
        }

        using var scope = services.CreateScope();
        var nicheRepo = scope.ServiceProvider.GetRequiredService<INicheProfileRepository>();
        var nicheJob = scope.ServiceProvider.GetRequiredService<NicheAnalysisBackgroundJob>();

        var stale = await nicheRepo.FailStaleProcessingAsync(StaleProcessingAge, ct);
        if (stale.IsSuccess && stale.Value > 0)
        {
            logger.LogWarning(
                "Marked {Count} stale niche analysis profile(s) as failed (no progress for {Minutes} min)",
                stale.Value,
                StaleProcessingAge.TotalMinutes);
        }

        var playwrightHolder = scope.ServiceProvider.GetService<PlaywrightBrowserHolder>();

        workerUser.UserId = serviceUserId;
        var queued = await nicheRepo.ListQueuedAsync(3, ct);
        if (!queued.IsSuccess || queued.Value is null || queued.Value.Count == 0)
        {
            workerUser.UserId = Guid.Empty;
            return;
        }

        foreach (var item in queued.Value)
        {
            try
            {
                workerUser.UserId = item.UserId;
                var claim = await nicheRepo.UpdateStatusAsync(
                    item.ProfileId, "processing", step: "schema", stepNumber: 1, totalSteps: 10, ct: ct);
                if (!claim.IsSuccess)
                {
                    logger.LogWarning(
                        "Could not claim niche profile {ProfileId}: {Error}",
                        item.ProfileId, claim.Error);
                    continue;
                }

                var payload = new NicheAnalysisJobPayload(item.ProfileId, item.UserId, item.Domain);
                using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                jobCts.CancelAfter(JobTimeout);
                await nicheJob.RunAsync(payload, jobCts.Token);
                logger.LogInformation("Niche analysis finished for profile {ProfileId}", item.ProfileId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Niche analysis failed for profile {ProfileId}", item.ProfileId);
            }
        }

        workerUser.UserId = Guid.Empty;
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
