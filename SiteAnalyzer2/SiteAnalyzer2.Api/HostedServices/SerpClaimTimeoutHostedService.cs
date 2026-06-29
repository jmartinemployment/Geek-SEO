using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services.Pipeline;

namespace SiteAnalyzer2.Api.HostedServices;

public sealed class SerpClaimTimeoutHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SerpClaimTimeoutHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!SerpExecutionConfiguration.IsExternal)
        {
            logger.LogInformation("SERP claim timeout service disabled (SERP_EXECUTION is not external).");
            return;
        }

        logger.LogInformation("SERP claim timeout service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FailExpiredUnclaimedRunsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "SERP claim timeout scan failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task FailExpiredUnclaimedRunsAsync(CancellationToken ct)
    {
        var timeoutSeconds = ResolveClaimTimeoutSeconds();
        var cutoff = DateTime.UtcNow.AddSeconds(-timeoutSeconds);
        var message =
            $"No SERP worker claimed this run within {timeoutSeconds} seconds. Start the SERP worker before beginning a run.";

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var runRepository = scope.ServiceProvider.GetRequiredService<IAnalysisRunRepository>();
        var runGate = scope.ServiceProvider.GetRequiredService<RunGateService>();

        var expiredRunIds = await db.AnalysisRuns
            .AsNoTracking()
            .Where(r => r.Status == RunStatus.Running && r.CurrentStage == PipelineStage.Serp)
            .Where(r => r.SerpClaimedAt == null && r.CreatedAt < cutoff)
            .Where(r => !db.RunGates.Any(g => g.RunId == r.Id && g.Stage == PipelineStage.Serp))
            .Select(r => r.Id)
            .ToListAsync(ct);

        foreach (var runId in expiredRunIds)
        {
            var run = await runRepository.GetByIdAsync(runId, ct);
            if (run is null
                || run.Status != RunStatus.Running
                || run.CurrentStage != PipelineStage.Serp
                || run.SerpClaimedAt.HasValue
                || run.RunGates.Any(g => g.Stage == PipelineStage.Serp))
            {
                continue;
            }

            await runGate.FailStageAsync(run, PipelineStage.Serp, message, ct);
            logger.LogWarning("Run {RunId} failed: unclaimed SERP timeout after {TimeoutSeconds}s.", runId, timeoutSeconds);
        }
    }

    private static int ResolveClaimTimeoutSeconds()
    {
        var raw = Environment.GetEnvironmentVariable("SERP_WORKER_CLAIM_TIMEOUT_SECONDS");
        return int.TryParse(raw, out var seconds) && seconds > 0 ? seconds : 90;
    }
}
