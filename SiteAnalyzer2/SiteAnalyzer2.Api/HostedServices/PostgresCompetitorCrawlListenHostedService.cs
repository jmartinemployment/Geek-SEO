using Npgsql;
using SiteAnalyzer2.Api.Realtime;

namespace SiteAnalyzer2.Api.HostedServices;

/// <summary>
/// LISTEN on PostgreSQL for crawl progress NOTIFY payloads, then fan out to local SSE subscribers.
/// </summary>
public sealed class PostgresCompetitorCrawlListenHostedService(
    string connectionString,
    CrawlProgressBroadcaster broadcaster,
    ILogger<PostgresCompetitorCrawlListenHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Competitor crawl LISTEN loop failed; reconnecting in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ListenLoopAsync(CancellationToken stoppingToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(stoppingToken);

        var backlog = new Queue<string>();

        connection.Notification += (_, args) =>
        {
            if (!string.Equals(args.Channel, PostgresCompetitorCrawlNotifier.Channel, StringComparison.Ordinal))
                return;

            lock (backlog)
            {
                backlog.Enqueue(args.Payload);
            }
        };

        await using (var listen = new NpgsqlCommand($"LISTEN {PostgresCompetitorCrawlNotifier.Channel}", connection))
        {
            await listen.ExecuteNonQueryAsync(stoppingToken);
        }

        logger.LogInformation("Listening for competitor crawl progress on PostgreSQL channel {Channel}.",
            PostgresCompetitorCrawlNotifier.Channel);

        while (!stoppingToken.IsCancellationRequested)
        {
            await connection.WaitAsync(stoppingToken);

            while (true)
            {
                string? payload;
                lock (backlog)
                {
                    if (backlog.Count == 0)
                        break;

                    payload = backlog.Dequeue();
                }

                if (!CompetitorCrawlPayloadParser.TryGetRunId(payload, out var runId))
                    continue;

                broadcaster.BroadcastToRun(runId.ToString(), payload);
            }
        }
    }
}
