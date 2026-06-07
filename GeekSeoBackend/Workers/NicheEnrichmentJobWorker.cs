namespace GeekSeoBackend.Workers;

/// <summary>
/// Picks up profiles with structure complete and enrichment pending (Phase 3 decoupled mode).
/// Idle until GeekRepository exposes maintenance/pending-enrichment and NICHE_DECOUPLED_ENRICHMENT=true.
/// </summary>
public sealed class NicheEnrichmentJobWorker(ILogger<NicheEnrichmentJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsDecoupledEnrichmentEnabled())
        {
            logger.LogDebug("NICHE_DECOUPLED_ENRICHMENT not enabled — enrichment worker idle");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static bool IsDecoupledEnrichmentEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("NICHE_DECOUPLED_ENRICHMENT"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
