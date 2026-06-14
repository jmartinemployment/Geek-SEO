using GeekSeo.Application.Interfaces;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Jobs;
using GeekSeoBackend.Services.NicheStepRunners;

namespace GeekSeoBackend.Workers;

public sealed class NicheAnalysisJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    NicheAnalysisJobChannel channel,
    ILogger<NicheAnalysisJobWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StaleProcessingAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(15);
    private const int BatchSize = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — NicheAnalysisJobWorker idle");
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
                logger.LogError(ex, "NicheAnalysisJobWorker iteration failed");
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
            var nicheRepo = scope.ServiceProvider.GetRequiredService<INicheProfileRepository>();

            var stale = await nicheRepo.FailStaleProcessingAsync(StaleProcessingAge, ct);
            if (stale.IsSuccess && stale.Value > 0)
                logger.LogWarning("Startup: marked {Count} stale niche profile(s) as failed", stale.Value);

            var queued = await nicheRepo.ListQueuedAsync(50, ct);
            if (queued.IsSuccess && queued.Value is { Count: > 0 })
            {
                foreach (var _ in queued.Value)
                    channel.Notify();
                logger.LogInformation("NicheAnalysisJobWorker: {Count} queued profile(s) found on startup", queued.Value.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "NicheAnalysisJobWorker startup drain failed");
        }
        finally
        {
            workerUser.UserId = Guid.Empty;
        }
    }

    private async Task ProcessQueuedAsync(Guid serviceUserId, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var nicheRepo = scope.ServiceProvider.GetRequiredService<INicheProfileRepository>();
        var nicheJob = scope.ServiceProvider.GetRequiredService<NicheAnalysisBackgroundJob>();

        var stale = await nicheRepo.FailStaleProcessingAsync(StaleProcessingAge, ct);
        if (stale.IsSuccess && stale.Value > 0)
            logger.LogWarning("Marked {Count} stale niche profile(s) as failed", stale.Value);

        var queued = await nicheRepo.ListQueuedAsync(BatchSize, ct);
        if (!queued.IsSuccess || queued.Value is null || queued.Value.Count == 0)
            return;

        foreach (var item in queued.Value)
        {
            try
            {
                workerUser.UserId = item.UserId;
                var claim = await nicheRepo.UpdateStatusAsync(
                    item.ProfileId, "processing", step: "schema", stepNumber: 1, totalSteps: NicheStepCatalog.Ordered.Count, ct: ct);
                if (!claim.IsSuccess)
                {
                    logger.LogWarning("Could not claim niche profile {ProfileId}: {Error}", item.ProfileId, claim.Error);
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

        workerUser.UserId = serviceUserId;
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
