using GeekSeo.Application.Interfaces;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Jobs;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Workers;

public sealed class SiteAnalysisJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    SiteAnalysisJobChannel channel,
    ILogger<SiteAnalysisJobWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StaleProcessingAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(15);
    private const int BatchSize = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — Site analysis worker idle");
            return;
        }

        await DrainExistingAsync(serviceUserId, stoppingToken);

        await foreach (var _ in channel.Reader.ReadAllAsync(stoppingToken))
        {
            workerUser.UserId = serviceUserId;
            try
            {
                await ProcessQueuedAsync(serviceUserId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SiteAnalysisJobWorker iteration failed");
            }
            finally
            {
                workerUser.UserId = Guid.Empty;
            }
        }
    }

    private async Task DrainExistingAsync(Guid serviceUserId, CancellationToken ct)
    {
        try
        {
            workerUser.UserId = serviceUserId;
            using var scope = services.CreateScope();
            var siteAnalysisRepo = scope.ServiceProvider.GetRequiredService<ISiteAnalysisProfileRepository>();

            var stale = await siteAnalysisRepo.FailStaleProcessingAsync(StaleProcessingAge, ct);
            if (stale.IsSuccess && stale.Value > 0)
                logger.LogWarning("Startup: marked {Count} stale site analysis profile(s) as failed", stale.Value);

            var queued = await siteAnalysisRepo.ListQueuedAsync(50, ct);
            if (queued.IsSuccess && queued.Value is { Count: > 0 })
            {
                foreach (var _ in queued.Value)
                    channel.Notify();
                logger.LogInformation("SiteAnalysisJobWorker: {Count} queued profile(s) found on startup", queued.Value.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SiteAnalysisJobWorker startup drain failed");
        }
        finally
        {
            workerUser.UserId = Guid.Empty;
        }
    }

    private async Task ProcessQueuedAsync(Guid serviceUserId, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var siteAnalysisRepo = scope.ServiceProvider.GetRequiredService<ISiteAnalysisProfileRepository>();
        var siteAnalysisJob = scope.ServiceProvider.GetRequiredService<SiteAnalysisBackgroundJob>();

        var stale = await siteAnalysisRepo.FailStaleProcessingAsync(StaleProcessingAge, ct);
        if (stale.IsSuccess && stale.Value > 0)
            logger.LogWarning("Marked {Count} stale site analysis profile(s) as failed", stale.Value);

        var queued = await siteAnalysisRepo.ListQueuedAsync(BatchSize, ct);
        if (!queued.IsSuccess || queued.Value is null || queued.Value.Count == 0)
            return;

        foreach (var item in queued.Value)
        {
            try
            {
                workerUser.UserId = item.UserId;
                var claim = await siteAnalysisRepo.UpdateStatusAsync(
                    item.ProfileId, "processing", step: "schema", stepNumber: 1, totalSteps: SiteAnalysisStepCatalog.Ordered.Count, ct: ct);
                if (!claim.IsSuccess)
                {
                    logger.LogWarning("Could not claim site analysis profile {ProfileId}: {Error}", item.ProfileId, claim.Error);
                    continue;
                }

                var payload = new SiteAnalysisJobPayload(item.ProfileId, item.UserId, item.Domain);
                using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                jobCts.CancelAfter(JobTimeout);
                await siteAnalysisJob.RunAsync(payload, jobCts.Token);
                logger.LogInformation("site analysis finished for profile {ProfileId}", item.ProfileId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "site analysis failed for profile {ProfileId}", item.ProfileId);
            }
        }

        workerUser.UserId = serviceUserId;
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
