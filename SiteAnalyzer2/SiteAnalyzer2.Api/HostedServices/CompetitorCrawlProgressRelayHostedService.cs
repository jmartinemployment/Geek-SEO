using Microsoft.Extensions.DependencyInjection;
using SiteAnalyzer2.Api.Realtime;
using SiteAnalyzer2.Services.CompetitorCrawl;

namespace SiteAnalyzer2.Api.HostedServices;

/// <summary>
/// Persists sequenced progress logs, then NOTIFY so every API replica can push to SSE subscribers.
/// </summary>
public sealed class CompetitorCrawlProgressRelayHostedService(
    CompetitorCrawlProgressPublisher publisher,
    PostgresCompetitorCrawlNotifier notifier,
    IServiceScopeFactory scopeFactory,
    ILogger<CompetitorCrawlProgressRelayHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var progress in publisher.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var logService = scope.ServiceProvider.GetRequiredService<CompetitorCrawlProgressLogService>();
                var payload = await logService.AppendAndBuildPayloadAsync(progress, stoppingToken);
                await notifier.NotifyRawAsync(payload, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to relay competitor crawl progress for run {RunId}", progress.RunId);
            }
        }
    }
}
