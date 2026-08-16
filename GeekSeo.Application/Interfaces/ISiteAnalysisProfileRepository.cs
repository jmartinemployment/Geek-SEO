using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces;

public interface ISiteAnalysisProfileRepository
{
    Task<Result<SiteAnalysisProfile>> CreateAsync(SiteAnalysisProfile profile, CancellationToken ct = default);
    Task<Result<SiteAnalysisProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default);
    /// <summary>Lightweight ownership check — returns only ProjectId without loading the pillar graph.</summary>
    Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<SiteAnalysisProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<SiteAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(
        Guid profileId, bool includeFusion, CancellationToken ct = default);
    Task<Result<SiteAnalysisProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> GetHistoryAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(
        Guid profileId, string status, string? step = null,
        int stepNumber = 0, int totalSteps = 0, string? errorMessage = null,
        SiteAnalysisStepLogEntry? stepLogEntry = null,
        CancellationToken ct = default);
    Task<Result> UpdateScoresAsync(Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default);
    Task<Result> UpdateProfileSummaryAsync(Guid profileId, SiteAnalysisProfileSummaryPatch summary, CancellationToken ct = default);
    Task<Result> SaveFusionSnapshotAsync(Guid profileId, string fusionSnapshotJson, CancellationToken ct = default);
    Task<Result> UpdatePhaseStatusAsync(Guid profileId, SiteAnalysisPhaseStatusPatch patch, CancellationToken ct = default);
    Task<Result> BulkUpsertTopicCandidatesAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisTopicCandidateBulkUpsert> candidates,
        string idempotencyKey,
        CancellationToken ct = default);
    Task<Result<SiteAnalysisTopicCandidateListResult>> GetTopicCandidatesAsync(
        Guid profileId,
        int page,
        int pageSize,
        bool? selectedOnly,
        CancellationToken ct = default);
    [Obsolete("Use split PATCH methods via SiteAnalysisPersistenceService. Retained for GeekRepository fallback.")]
    Task<Result> SaveAnalysisResultsAsync(Guid profileId, SiteAnalysisSaveRequest results, CancellationToken ct = default);
    Task<Result> BulkInsertPillarsAsync(IEnumerable<SiteAnalysisPillar> pillars, CancellationToken ct = default);
    Task<Result> BulkInsertSubtopicsAsync(IEnumerable<SiteAnalysisSubtopic> subtopics, CancellationToken ct = default);
    Task<Result> BulkInsertCompetitorsAsync(IEnumerable<SiteAnalysisCompetitor> competitors, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisCompetitor>>> GetCompetitorsAsync(Guid profileId, CancellationToken ct = default);
    Task<Result> UpdateCompetitorInsightsAsync(SiteAnalysisCompetitor competitor, CancellationToken ct = default);
    Task<Result> BulkInsertEntitiesAsync(IEnumerable<SiteAnalysisEntity> entities, CancellationToken ct = default);
    Task<Result> BulkInsertPillarPagesAsync(IEnumerable<SiteAnalysisPillarPage> pages, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default);
    Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default);

    Task<Result> UpsertStepRunAsync(
        Guid profileId,
        SiteAnalysisProfileStepRunUpsert stepRun,
        CancellationToken ct = default);
    Task<Result> UpdateStepRunStatusAsync(
        Guid profileId,
        string stepSlug,
        SiteAnalysisProfileStepRunStatusPatch patch,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileStepRunRow>>> GetStepRunsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceSchemaSignalsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileSchemaSignalWrite> signals,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileSchemaSignalRow>>> GetSchemaSignalsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceDiscoveredUrlsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileDiscoveredUrlWrite> urls,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceNavigationLinksAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileNavigationLinkWrite> links,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>> GetNavigationLinksAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceHeadingsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileHeadingWrite> headings,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileHeadingRow>>> GetHeadingsAsync(
        Guid profileId,
        CancellationToken ct = default);

    /// <summary>Real per-page heading+paragraph tree (see PageSection), replacing the flat
    /// heading model above for gap/pillar detection use.</summary>
    Task<Result> ReplacePageSectionTreesAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisPageSectionTreeWrite> pages,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>> GetPageSectionTreesAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceExtractedToolsAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisProfileExtractedToolWrite> tools,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisProfileExtractedToolRow>>> GetExtractedToolsAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceTopicCandidateEvidenceAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisTopicCandidateEvidenceWrite> evidence,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<SiteAnalysisTopicCandidateEvidenceRow>>> GetTopicCandidateEvidenceAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplacePageContentAsync(
        Guid profileId,
        SiteAnalysisProfilePageContentWrite content,
        CancellationToken ct = default);
    Task<Result<SiteAnalysisProfilePageContentRow?>> GetPageContentAsync(
        Guid profileId,
        CancellationToken ct = default);

    Task<Result> ReplaceSiteStructureAsync(
        Guid profileId,
        SiteAnalysisProfileSiteStructureWrite structure,
        CancellationToken ct = default);
    Task<Result<SiteAnalysisProfileSiteStructureRow?>> GetSiteStructureAsync(
        Guid profileId,
        CancellationToken ct = default);

    // Step isolation
    Task<Result> UpdateStepStatusAsync(Guid profileId, string slug, string status,
        SiteAnalysisStepLogEntry? entry = null, CancellationToken ct = default);
    Task<Result> InvalidateDownstreamStepsAsync(Guid profileId,
        IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default);
    Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson,
        CancellationToken ct = default);
    Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(
        Guid profileId, CancellationToken ct = default);
}
