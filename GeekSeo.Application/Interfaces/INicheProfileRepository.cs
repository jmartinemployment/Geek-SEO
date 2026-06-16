using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces;

public interface INicheProfileRepository
{
    Task<Result<NicheProfile>> CreateAsync(NicheProfile profile, CancellationToken ct = default);
    Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default);
    /// <summary>Lightweight ownership check — returns only ProjectId without loading the pillar graph.</summary>
    Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<NicheProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<NicheAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(
        Guid profileId, bool includeFusion, CancellationToken ct = default);
    Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(
        Guid profileId, string status, string? step = null,
        int stepNumber = 0, int totalSteps = 0, string? errorMessage = null,
        NicheAnalysisStepLogEntry? stepLogEntry = null,
        CancellationToken ct = default);
    Task<Result> UpdateScoresAsync(Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default);
    Task<Result> UpdateProfileSummaryAsync(Guid profileId, NicheProfileSummaryPatch summary, CancellationToken ct = default);
    Task<Result> SaveFusionSnapshotAsync(Guid profileId, string fusionSnapshotJson, CancellationToken ct = default);
    Task<Result> UpdatePhaseStatusAsync(Guid profileId, NichePhaseStatusPatch patch, CancellationToken ct = default);
    Task<Result> BulkUpsertTopicCandidatesAsync(
        Guid profileId,
        IReadOnlyList<NicheTopicCandidateBulkUpsert> candidates,
        string idempotencyKey,
        CancellationToken ct = default);
    Task<Result<NicheTopicCandidateListResult>> GetTopicCandidatesAsync(
        Guid profileId,
        int page,
        int pageSize,
        bool? selectedOnly,
        CancellationToken ct = default);
    [Obsolete("Use split PATCH methods via NicheAnalysisPersistenceService. Retained for GeekRepository fallback.")]
    Task<Result> SaveAnalysisResultsAsync(Guid profileId, NicheAnalysisSaveRequest results, CancellationToken ct = default);
    Task<Result> BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default);
    Task<Result> BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default);
    Task<Result> BulkInsertCompetitorsAsync(IEnumerable<NicheCompetitor> competitors, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheCompetitor>>> GetCompetitorsAsync(Guid profileId, CancellationToken ct = default);
    Task<Result> UpdateCompetitorInsightsAsync(NicheCompetitor competitor, CancellationToken ct = default);
    Task<Result> BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default);
    Task<Result> BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default);
    Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default);

    Task<Result> UpsertStepRunAsync(
        Guid profileId,
        NicheProfileStepRunUpsert stepRun,
        CancellationToken ct = default);
    Task<Result> UpdateStepRunStatusAsync(
        Guid profileId,
        string stepSlug,
        NicheProfileStepRunStatusPatch patch,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileStepRunRow>>> GetStepRunsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceSchemaSignalsAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileSchemaSignalWrite> signals,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileSchemaSignalRow>>> GetSchemaSignalsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceDiscoveredUrlsAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileDiscoveredUrlWrite> urls,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceNavigationLinksAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileNavigationLinkWrite> links,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileNavigationLinkRow>>> GetNavigationLinksAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceHeadingsAsync(
        Guid profileId,
        IReadOnlyList<NicheProfileHeadingWrite> headings,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileHeadingRow>>> GetHeadingsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceTopicCandidateEvidenceAsync(
        Guid profileId,
        IReadOnlyList<NicheTopicCandidateEvidenceWrite> evidence,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheTopicCandidateEvidenceRow>>> GetTopicCandidateEvidenceAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplacePageContentAsync(
        Guid profileId,
        NicheProfilePageContentWrite content,
        CancellationToken ct = default);
    Task<Result<NicheProfilePageContentRow?>> GetPageContentAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceSiteStructureAsync(
        Guid profileId,
        NicheProfileSiteStructureWrite structure,
        CancellationToken ct = default);
    Task<Result<NicheProfileSiteStructureRow?>> GetSiteStructureAsync(
        Guid profileId,
        CancellationToken ct = default);

    // Step isolation
    Task<Result> UpdateStepStatusAsync(Guid profileId, string slug, string status,
        NicheAnalysisStepLogEntry? entry = null, CancellationToken ct = default);
    Task<Result> InvalidateDownstreamStepsAsync(Guid profileId,
        IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default);
    Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson,
        CancellationToken ct = default);
    Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(
        Guid profileId, CancellationToken ct = default);
}
