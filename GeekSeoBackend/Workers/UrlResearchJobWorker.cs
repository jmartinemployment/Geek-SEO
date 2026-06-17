using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class UrlResearchJobWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    UrlResearchJobChannel channel,
    ILogger<UrlResearchJobWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StaleRunningAge = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — UrlResearchJobWorker idle");
            return;
        }

        await DrainExistingAsync(serviceUserId, stoppingToken);

        await foreach (var _ in channel.Reader.ReadAllAsync(stoppingToken))
        {
            workerUser.UserId = serviceUserId;
            try
            {
                using var scope = services.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<UrlResearchJobProcessor>();
                await processor.ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "UrlResearchJobWorker iteration failed");
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
            var repo = scope.ServiceProvider.GetRequiredService<IUrlResearchRepository>();

            var stale = await repo.FailStaleRunningAsync(StaleRunningAge, ct);
            if (stale.IsSuccess && stale.Value > 0)
                logger.LogWarning("Startup: marked {Count} stale url research job(s) as failed", stale.Value);

            var queued = await repo.ListQueuedAsync(50, ct);
            if (queued.IsSuccess && queued.Value is { Count: > 0 })
            {
                foreach (var _ in queued.Value)
                    channel.Notify();
                logger.LogInformation(
                    "UrlResearchJobWorker: {Count} queued job(s) found on startup",
                    queued.Value.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "UrlResearchJobWorker startup drain failed");
        }
        finally
        {
            workerUser.UserId = Guid.Empty;
        }
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
