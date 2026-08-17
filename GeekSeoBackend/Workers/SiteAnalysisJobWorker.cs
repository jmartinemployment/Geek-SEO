using GeekSeo.Application.Interfaces;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Jobs;
using GeekSeoBackend.Services;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogWarning("WORKER_SERVICE_USER_ID not set — Site analysis worker idle");
            return;
        }

        await DrainExistingAsync(serviceUserId, stoppingToken);

        await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                workerUser.UserId = job.UserId;
                using var scope = services.CreateScope();
                var siteAnalysisJob = scope.ServiceProvider.GetRequiredService<SiteAnalysisBackgroundJob>();
                using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                jobCts.CancelAfter(JobTimeout);
                await siteAnalysisJob.RunThroughCoverageInMemoryAsync(job, jobCts.Token);
                logger.LogInformation("Through coverage finished for project {ProjectId} domain {Domain}", job.ProjectId, job.Domain);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Through coverage failed for project {ProjectId} domain {Domain}", job.ProjectId, job.Domain);
            }
            finally
            {
                workerUser.UserId = Guid.Empty;
            }
        }
    }

    /// <summary>Leftover queued DB profiles from the old persist-per-step path.</summary>
    private async Task DrainExistingAsync(Guid serviceUserId, CancellationToken ct)
    {
        try
        {
            workerUser.UserId = serviceUserId;
            using var scope = services.CreateScope();
            var siteAnalysisRepo = scope.ServiceProvider.GetRequiredService<ISiteAnalysisProfileRepository>();
            var siteAnalysisJob = scope.ServiceProvider.GetRequiredService<SiteAnalysisBackgroundJob>();

            var stale = await siteAnalysisRepo.FailStaleProcessingAsync(StaleProcessingAge, ct);
            if (stale.IsSuccess && stale.Value > 0)
                logger.LogWarning("Startup: marked {Count} stale site analysis profile(s) as failed", stale.Value);

            var queued = await siteAnalysisRepo.ListQueuedAsync(50, ct);
            if (!queued.IsSuccess || queued.Value is null || queued.Value.Count == 0)
                return;

            foreach (var item in queued.Value)
            {
                try
                {
                    workerUser.UserId = item.UserId;
                    var claim = await siteAnalysisRepo.UpdateStatusAsync(
                        item.ProfileId, "processing", step: "schema", stepNumber: 1,
                        totalSteps: SiteAnalyzerStepCatalog.ThroughCoverage.Count, ct: ct);
                    if (!claim.IsSuccess)
                        continue;
                    using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    jobCts.CancelAfter(JobTimeout);
                    await siteAnalysisJob.RunAsync(
                        new SiteAnalysisJobPayload(item.ProfileId, item.UserId, item.Domain),
                        jobCts.Token);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Leftover queued profile {ProfileId} failed", item.ProfileId);
                }
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

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
