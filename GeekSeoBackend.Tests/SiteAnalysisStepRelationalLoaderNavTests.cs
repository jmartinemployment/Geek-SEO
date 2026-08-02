using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerStepRelationalLoaderNavTests
{
    [Fact]
    public async Task LoadNavAsync_returns_skipped_empty_nav_when_links_and_artifact_missing()
    {
        var repo = new NavOnlyRepo();
        var steps = new List<SiteAnalysisStepLogEntry>
        {
            new(
                3,
                "nav",
                "Navigation",
                "complete",
                "Navigation step skipped — browser unavailable.",
                new Dictionary<string, object?>()),
        };

        var nav = await SiteAnalyzerStepRelationalLoader.LoadNavAsync(
            repo,
            Guid.NewGuid(),
            "https://example.com",
            steps,
            CancellationToken.None);

        Assert.Empty(nav.Pillars);
        Assert.Equal("skipped", nav.ExtractMethod);
    }

    private sealed class NavOnlyRepo : ISiteAnalysisProfileRepository
    {
        public Task<Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>> GetNavigationLinksAsync(
            Guid profileId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>.Success([]));

        public Task<Result<SiteAnalysisProfile>> CreateAsync(SiteAnalysisProfile profile, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<SiteAnalysisProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<SiteAnalysisProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<SiteAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(
            Guid profileId, bool includeFusion, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<SiteAnalysisProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> GetHistoryAsync(
            Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpdateStatusAsync(
            Guid profileId, string status, string? step = null, int stepNumber = 0, int totalSteps = 0,
            string? errorMessage = null, SiteAnalysisStepLogEntry? stepLogEntry = null, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpdateScoresAsync(
            Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpdateProfileSummaryAsync(
            Guid profileId, SiteAnalysisProfileSummaryPatch summary, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> SaveFusionSnapshotAsync(
            Guid profileId, string fusionSnapshotJson, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpdatePhaseStatusAsync(Guid profileId, SiteAnalysisPhaseStatusPatch patch, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> BulkUpsertTopicCandidatesAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisTopicCandidateBulkUpsert> candidates, string idempotencyKey, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<SiteAnalysisTopicCandidateListResult>> GetTopicCandidatesAsync(
            Guid profileId, int page, int pageSize, bool? selectedOnly, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> SaveAnalysisResultsAsync(
            Guid profileId, SiteAnalysisSaveRequest results, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> BulkInsertPillarsAsync(IEnumerable<SiteAnalysisPillar> pillars, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> BulkInsertSubtopicsAsync(IEnumerable<SiteAnalysisSubtopic> subtopics, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> BulkInsertCompetitorsAsync(IEnumerable<SiteAnalysisCompetitor> competitors, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisCompetitor>>> GetCompetitorsAsync(Guid profileId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SiteAnalysisCompetitor>>.Success(Array.Empty<SiteAnalysisCompetitor>()));
        public Task<Result> UpdateCompetitorInsightsAsync(SiteAnalysisCompetitor competitor, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> BulkInsertEntitiesAsync(IEnumerable<SiteAnalysisEntity> entities, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> BulkInsertPillarPagesAsync(IEnumerable<SiteAnalysisPillarPage> pages, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpsertStepRunAsync(
            Guid profileId, SiteAnalysisProfileStepRunUpsert stepRun, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpdateStepRunStatusAsync(
            Guid profileId, string stepSlug, SiteAnalysisProfileStepRunStatusPatch patch, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileStepRunRow>>> GetStepRunsAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplaceSchemaSignalsAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisProfileSchemaSignalWrite> signals, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileSchemaSignalRow>>> GetSchemaSignalsAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplaceDiscoveredUrlsAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisProfileDiscoveredUrlWrite> urls, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplaceNavigationLinksAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisProfileNavigationLinkWrite> links, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplaceHeadingsAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisProfileHeadingWrite> headings, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileHeadingRow>>> GetHeadingsAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplaceTopicCandidateEvidenceAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisTopicCandidateEvidenceWrite> evidence, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisTopicCandidateEvidenceRow>>> GetTopicCandidateEvidenceAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplacePageContentAsync(
            Guid profileId, SiteAnalysisProfilePageContentWrite content, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<SiteAnalysisProfilePageContentRow?>> GetPageContentAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplaceSiteStructureAsync(
            Guid profileId, SiteAnalysisProfileSiteStructureWrite structure, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<SiteAnalysisProfileSiteStructureRow?>> GetSiteStructureAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpdateStepStatusAsync(
            Guid profileId, string slug, string status, SiteAnalysisStepLogEntry? entry = null, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> InvalidateDownstreamStepsAsync(
            Guid profileId, IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> UpdateCrawledUrlsAsync(
            Guid profileId, string crawledUrlsJson, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
