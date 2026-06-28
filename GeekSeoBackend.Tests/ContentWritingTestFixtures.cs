using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

internal static class ContentWritingTestFixtures
{
    internal static SiteWritingFocusAssembler CreateSiteFocusAssembler(IProjectRepository projects) =>
        new(projects, new NullNicheProfileRepository(), new NullNicheAnalyticsRepository(), new NullSiteResearchRepository());

    private sealed class NullNicheProfileRepository : INicheProfileRepository
    {
        public Task<Result<NicheProfile>> CreateAsync(NicheProfile profile, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(Guid profileId, bool includeFusion, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<NicheProfile?>.Success(null));
        public Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(Guid projectId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpsertStepRunAsync(Guid profileId, NicheProfileStepRunUpsert stepRun, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateStepRunStatusAsync(Guid profileId, string stepSlug, NicheProfileStepRunStatusPatch patch, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileStepRunRow>>> GetStepRunsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceSchemaSignalsAsync(Guid profileId, IReadOnlyList<NicheProfileSchemaSignalWrite> signals, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileSchemaSignalRow>>> GetSchemaSignalsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceDiscoveredUrlsAsync(Guid profileId, IReadOnlyList<NicheProfileDiscoveredUrlWrite> urls, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceNavigationLinksAsync(Guid profileId, IReadOnlyList<NicheProfileNavigationLinkWrite> links, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileNavigationLinkRow>>> GetNavigationLinksAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceHeadingsAsync(Guid profileId, IReadOnlyList<NicheProfileHeadingWrite> headings, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileHeadingRow>>> GetHeadingsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceTopicCandidateEvidenceAsync(Guid profileId, IReadOnlyList<NicheTopicCandidateEvidenceWrite> evidence, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheTopicCandidateEvidenceRow>>> GetTopicCandidateEvidenceAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplacePageContentAsync(Guid profileId, NicheProfilePageContentWrite content, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfilePageContentRow?>> GetPageContentAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceSiteStructureAsync(Guid profileId, NicheProfileSiteStructureWrite structure, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfileSiteStructureRow?>> GetSiteStructureAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateStatusAsync(Guid profileId, string status, string? step = null, int stepNumber = 0, int totalSteps = 0, string? errorMessage = null, NicheAnalysisStepLogEntry? stepLogEntry = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateScoresAsync(Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateProfileSummaryAsync(Guid profileId, NicheProfileSummaryPatch summary, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveFusionSnapshotAsync(Guid profileId, string fusionSnapshotJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdatePhaseStatusAsync(Guid profileId, NichePhaseStatusPatch patch, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkUpsertTopicCandidatesAsync(Guid profileId, IReadOnlyList<NicheTopicCandidateBulkUpsert> candidates, string idempotencyKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheTopicCandidateListResult>> GetTopicCandidatesAsync(Guid profileId, int page, int pageSize, bool? selectedOnly, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveAnalysisResultsAsync(Guid profileId, NicheAnalysisSaveRequest results, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertCompetitorsAsync(IEnumerable<NicheCompetitor> competitors, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheCompetitor>>> GetCompetitorsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateCompetitorInsightsAsync(NicheCompetitor competitor, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateStepStatusAsync(Guid profileId, string slug, string status, NicheAnalysisStepLogEntry? entry = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> InvalidateDownstreamStepsAsync(Guid profileId, IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    internal sealed class NullSiteAnalyzer2SiteProfileRepository : ISiteAnalyzer2SiteProfileRepository
    {
        public Task<Result<SiteAnalyzer2SiteProfileExport>> GetByIdAsync(Guid siteProfileId, CancellationToken ct = default) =>
            Task.FromResult(Result<SiteAnalyzer2SiteProfileExport>.NotFound("Site profile not found"));

        public Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleAsync(Guid siteProfileId, CancellationToken ct = default) =>
            Task.FromResult(Result<ContentWriterSiteBundle>.NotFound("Site profile not found"));

        public Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleByGeekSeoProjectIdAsync(Guid geekSeoProjectId, CancellationToken ct = default) =>
            Task.FromResult(Result<ContentWriterSiteBundle>.NotFound("Site profile not found"));
    }

    internal sealed class NullNicheAnalyticsRepository : INicheAnalyticsDapperRepository
    {
        public Task<Result<NicheProfileSummary?>> GetProfileSummaryAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<PillarCoverageMatrix>>> GetCoverageMatrixAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<TopicalGapSummary>>> GetTopicalGapsAsync(Guid profileId, bool quickWinsOnly = false, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<TopicalGapSummary>>.Success([]));
        public Task<Result<IReadOnlyList<AuthorityProgressPoint>>> GetAuthorityProgressAsync(Guid projectId, int months = 12, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<CompetitorNicheOverlap>>> GetCompetitorOverlapAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<EntityCoverageReport>>> GetEntityCoverageAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class NullSiteResearchRepository : ISiteResearchRepository
    {
        public Task<Result<SeoSiteResearch>> GetOrCreateForProjectAsync(
            Guid userId, CreateSiteResearchRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSiteResearch>.Success(new SeoSiteResearch
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                UserId = userId,
                SiteUrl = request.SiteUrl,
                BusinessSummary = string.Empty,
            }));

        public Task<Result<SeoSiteResearch>> GetWithPagesAsync(Guid siteResearchId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SeoSiteResearch>> PersistStep1Async(Guid siteResearchId, SiteResearchStep1Write body, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SeoSiteResearch>> ReplacePagesAsync(Guid siteResearchId, IReadOnlyList<SiteResearchPageWrite> pages, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SeoSiteResearch>> PersistStep4Async(Guid siteResearchId, SiteResearchStep4Write body, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpsertStepRunAsync(SiteAnalyzerStepRunUpsert upsert, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalyzerStepRunRow>>> GetStepRunsForSiteAsync(Guid siteResearchId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalyzerStepRunRow>>> GetStepRunsForPackAsync(Guid urlResearchId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
