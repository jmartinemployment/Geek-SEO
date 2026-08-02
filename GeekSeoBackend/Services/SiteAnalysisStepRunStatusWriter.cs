using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Services;

/// <summary>
/// Keeps relational <c>site_analysis_profile_step_runs</c> in sync with legacy step-status writes.
/// </summary>
internal static class SiteAnalyzerStepRunStatusWriter
{
    public static async Task SyncAsync(
        ISiteAnalysisProfileRepository profileRepo,
        ILogger logger,
        Guid profileId,
        string slug,
        string status,
        SiteAnalysisStepDefinition? definition = null,
        SiteAnalysisStepLogEntry? entry = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        SiteAnalysisStepCatalog.BySlug.TryGetValue(slug, out var stepDef);
        definition ??= stepDef;
        var now = DateTimeOffset.UtcNow;

        var patch = status switch
        {
            "running" => new SiteAnalysisProfileStepRunStatusPatch("running", HeartbeatAt: now, Summary: string.Empty),
            "complete" => new SiteAnalysisProfileStepRunStatusPatch(
                "complete",
                CompletedAt: now,
                Summary: entry?.Summary),
            "error" => new SiteAnalysisProfileStepRunStatusPatch(
                "error",
                CompletedAt: now,
                ErrorMessage: errorMessage ?? entry?.Summary,
                Summary: entry?.Summary),
            _ => new SiteAnalysisProfileStepRunStatusPatch(status),
        };

        var update = await profileRepo.UpdateStepRunStatusAsync(profileId, slug, patch, ct);
        if (update.IsSuccess || definition is null)
            return;

        var upsert = await profileRepo.UpsertStepRunAsync(
            profileId,
            new SiteAnalysisProfileStepRunUpsert(
                definition.StepNumber,
                slug,
                status,
                StartedAt: status == "running" ? now : null,
                HeartbeatAt: status == "running" ? now : null,
                CompletedAt: status is "complete" or "error" ? now : null,
                ErrorMessage: errorMessage,
                Summary: status == "running" ? string.Empty : entry?.Summary),
            ct);
        if (!upsert.IsSuccess)
        {
            logger.LogWarning(
                "Could not sync step run row for profile {ProfileId} slug {Slug}: {Error}",
                profileId,
                slug,
                upsert.Error);
        }
    }
}
