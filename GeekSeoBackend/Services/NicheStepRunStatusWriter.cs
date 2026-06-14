using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.NicheStepRunners;

namespace GeekSeoBackend.Services;

/// <summary>
/// Keeps relational <c>niche_profile_step_runs</c> in sync with legacy step-status writes.
/// </summary>
internal static class NicheStepRunStatusWriter
{
    public static async Task SyncAsync(
        INicheProfileRepository profileRepo,
        ILogger logger,
        Guid profileId,
        string slug,
        string status,
        NicheStepDefinition? definition = null,
        NicheAnalysisStepLogEntry? entry = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        NicheStepCatalog.BySlug.TryGetValue(slug, out var stepDef);
        definition ??= stepDef;
        var now = DateTimeOffset.UtcNow;

        var patch = status switch
        {
            "running" => new NicheProfileStepRunStatusPatch("running", HeartbeatAt: now),
            "complete" => new NicheProfileStepRunStatusPatch(
                "complete",
                CompletedAt: now,
                Summary: entry?.Summary),
            "error" => new NicheProfileStepRunStatusPatch(
                "error",
                CompletedAt: now,
                ErrorMessage: errorMessage ?? entry?.Summary,
                Summary: entry?.Summary),
            _ => new NicheProfileStepRunStatusPatch(status),
        };

        var update = await profileRepo.UpdateStepRunStatusAsync(profileId, slug, patch, ct);
        if (update.IsSuccess || definition is null)
            return;

        var upsert = await profileRepo.UpsertStepRunAsync(
            profileId,
            new NicheProfileStepRunUpsert(
                definition.StepNumber,
                slug,
                status,
                StartedAt: status == "running" ? now : null,
                HeartbeatAt: status == "running" ? now : null,
                CompletedAt: status is "complete" or "error" ? now : null,
                ErrorMessage: errorMessage,
                Summary: entry?.Summary),
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
