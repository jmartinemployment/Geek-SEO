using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using Microsoft.Extensions.Logging;

namespace GeekSeoBackend.Services;

/// <summary>
/// Orchestrates split PATCH writes for niche analysis completion (Phase 1+).
/// Falls back to monolithic <see cref="INicheProfileRepository.SaveAnalysisResultsAsync"/> when split routes are unavailable.
/// </summary>
public sealed class NicheAnalysisPersistenceService(
    INicheProfileRepository profileRepo,
    ILogger<NicheAnalysisPersistenceService> logger)
{
    private const int CandidateBatchSize = 200;

    public async Task<Result> PersistCandidatesAsync(
        Guid profileId,
        SiteTopicProfile fused,
        bool includeEvidence,
        CancellationToken ct = default)
    {
        var rows = GeekSeo.Application.Mapping.NicheTopicCandidateMapper
            .FromSiteTopicProfile(profileId, fused, includeEvidence);
        if (rows.Count == 0)
            return Result.Success();

        for (var offset = 0; offset < rows.Count; offset += CandidateBatchSize)
        {
            var batch = rows.Skip(offset).Take(CandidateBatchSize).ToList();
            var idempotencyKey = $"{profileId:N}:candidates:{offset / CandidateBatchSize}";
            var batchResult = await profileRepo.BulkUpsertTopicCandidatesAsync(
                profileId, batch, idempotencyKey, ct);
            if (!batchResult.IsSuccess)
            {
                if (IsRouteUnavailable(batchResult.Error))
                {
                    logger.LogWarning(
                        "Topic candidate UPSERT not available for {ProfileId} — skipping until GeekRepository deploys",
                        profileId);
                    return Result.Success();
                }

                return batchResult;
            }
        }

        if (includeEvidence)
        {
            var persistedCandidates = await LoadAllPersistedCandidatesAsync(profileId, ct);
            if (!persistedCandidates.IsSuccess)
                return Result.Failure(persistedCandidates.Error ?? "Failed to read persisted topic candidates.");

            var candidateIdsBySlug = (persistedCandidates.Value ?? [])
                .GroupBy(x => x.Slug, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var evidenceRows = fused.AllCandidates
                .Where(c => candidateIdsBySlug.ContainsKey(c.Slug))
                .SelectMany(c => c.Evidence.Select((e, index) => new NicheTopicCandidateEvidenceWrite(
                    candidateIdsBySlug[c.Slug],
                    e.Source,
                    e.Url,
                    e.Snippet,
                    e.Snippet ?? c.Name,
                    index)))
                .ToList();

            var evidenceResult = await profileRepo.ReplaceTopicCandidateEvidenceAsync(profileId, evidenceRows, ct);
            if (!evidenceResult.IsSuccess)
                return evidenceResult;
        }

        await profileRepo.UpdatePhaseStatusAsync(
            profileId,
            new NichePhaseStatusPatch(null, null, PersistStage: "candidates"),
            ct);
        return Result.Success();
    }

    private async Task<Result<IReadOnlyList<NicheTopicCandidatePage>>> LoadAllPersistedCandidatesAsync(
        Guid profileId,
        CancellationToken ct)
    {
        const int pageSize = 500;
        var page = 1;
        var items = new List<NicheTopicCandidatePage>();

        while (true)
        {
            var result = await profileRepo.GetTopicCandidatesAsync(
                profileId,
                page,
                pageSize,
                selectedOnly: null,
                ct);
            if (!result.IsSuccess)
                return Result<IReadOnlyList<NicheTopicCandidatePage>>.Failure(result.Error ?? "Failed loading topic candidates.");
            if (result.Value is null)
                return Result<IReadOnlyList<NicheTopicCandidatePage>>.Failure("Topic candidate response was empty.");

            items.AddRange(result.Value.Items);
            if (items.Count >= result.Value.Total || result.Value.Items.Count == 0)
                break;

            page++;
        }

        return Result<IReadOnlyList<NicheTopicCandidatePage>>.Success(items);
    }

    public async Task<Result> SaveCompletionAsync(
        Guid profileId,
        NicheProfileSummaryPatch summary,
        decimal authorityScore,
        int covered,
        int partial,
        int gap,
        SiteTopicProfile? fusedForArchive,
        bool writeFusionArchive,
        CancellationToken ct = default)
    {
        var summaryResult = await profileRepo.UpdateProfileSummaryAsync(profileId, summary, ct);
        var scoresResult = await profileRepo.UpdateScoresAsync(
            profileId, authorityScore, covered, partial, gap, ct);

        if (IsRouteUnavailable(summaryResult.Error))
        {
            logger.LogWarning(
                "Split profile-summary PATCH unavailable for {ProfileId} — falling back to analysis-results",
                profileId);
            return await FallbackMonolithicSaveAsync(
                profileId, summary, authorityScore, covered, partial, gap, fusedForArchive, writeFusionArchive, ct);
        }

        if (!summaryResult.IsSuccess)
            return summaryResult;
        if (!scoresResult.IsSuccess)
            return scoresResult;

        if (writeFusionArchive && fusedForArchive is not null)
        {
            var fusionJson = SiteTopicProfileJson.SerializeForPersistence(fusedForArchive);
            var fusionResult = await profileRepo.SaveFusionSnapshotAsync(profileId, fusionJson, ct);
            if (!fusionResult.IsSuccess && !IsRouteUnavailable(fusionResult.Error))
                return fusionResult;
        }

        return Result.Success();
    }

    private async Task<Result> FallbackMonolithicSaveAsync(
        Guid profileId,
        NicheProfileSummaryPatch summary,
        decimal authorityScore,
        int covered,
        int partial,
        int gap,
        SiteTopicProfile? fusedForArchive,
        bool writeFusionArchive,
        CancellationToken ct)
    {
        var fusionJson = writeFusionArchive && fusedForArchive is not null
            ? SiteTopicProfileJson.SerializeForPersistence(fusedForArchive)
            : null;

#pragma warning disable CS0618
        return await profileRepo.SaveAnalysisResultsAsync(profileId, new NicheAnalysisSaveRequest(
#pragma warning restore CS0618
            summary.PrimaryNiche,
            summary.NicheDescription,
            summary.NicheTags,
            summary.AudienceType,
            string.Empty,
            authorityScore,
            summary.TotalPillarsIdentified,
            covered,
            partial,
            gap,
            summary.AnalyzedAt,
            summary.NextAnalysisDue,
            fusionJson), ct);
    }

    private static bool IsRouteUnavailable(string? error) =>
        error is not null && (
            error.Contains("404", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("NotFound", StringComparison.OrdinalIgnoreCase));
}
