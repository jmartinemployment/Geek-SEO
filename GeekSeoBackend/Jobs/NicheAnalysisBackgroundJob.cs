using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Jobs;

public sealed record NicheAnalysisJobPayload(Guid ProfileId, Guid UserId, string Domain);

public sealed class NicheAnalysisBackgroundJob(
    NicheAnalyzerService analyzerService,
    PlaywrightBrowserHolder? playwrightHolder,
    ILogger<NicheAnalysisBackgroundJob> logger)
{
    public async Task RunAsync(NicheAnalysisJobPayload payload, CancellationToken ct)
    {
        logger.LogInformation(
            "Starting niche analysis job for profile {ProfileId} domain {Domain}",
            payload.ProfileId, payload.Domain);

        var browser = playwrightHolder?.Browser;
        await analyzerService.RunAnalysisAsync(payload.ProfileId, payload.UserId, browser, ct);
    }
}
