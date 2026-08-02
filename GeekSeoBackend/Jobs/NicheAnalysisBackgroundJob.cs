using GeekSeo.Application.Interfaces;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Jobs;

public sealed record NicheAnalysisJobPayload(Guid ProfileId, Guid UserId, string Domain);

public sealed class NicheAnalysisBackgroundJob(
    NicheAnalyzerService analyzerService,
    INicheProfileRepository profileRepo,
    PlaywrightBrowserHolder? playwrightHolder,
    ILogger<NicheAnalysisBackgroundJob> logger)
{
    public async Task RunAsync(NicheAnalysisJobPayload payload, CancellationToken ct)
    {
        var browser = playwrightHolder?.Browser;
        var profileResult = await profileRepo.GetByIdAsync(payload.ProfileId, ct);
        var persistStage = profileResult.IsSuccess ? profileResult.Value?.PersistStage : null;
        var siteCoverage = string.Equals(
            persistStage,
            SiteAnalyzerStepCatalog.SiteCoveragePersistStage,
            StringComparison.OrdinalIgnoreCase);

        if (siteCoverage)
        {
            logger.LogInformation(
                "Starting site analysis (through coverage) for profile {ProfileId} domain {Domain}",
                payload.ProfileId, payload.Domain);
            await analyzerService.RunThroughCoverageAsync(payload.ProfileId, payload.UserId, browser, ct);
            return;
        }

        logger.LogInformation(
            "Starting full site analysis job for profile {ProfileId} domain {Domain}",
            payload.ProfileId, payload.Domain);
        await analyzerService.RunAnalysisAsync(payload.ProfileId, payload.UserId, browser, ct);
    }
}
