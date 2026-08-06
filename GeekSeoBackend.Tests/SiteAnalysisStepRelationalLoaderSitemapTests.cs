using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerStepRelationalLoaderSitemapTests
{
    [Fact]
    public async Task LoadSitemapAsync_throws_when_no_inventory_persisted()
    {
        // Fail closed: step 1 (sitemap generation) always persists a non-empty inventory or
        // throws, so an empty result here means step 1 hasn't actually run for this profile —
        // that must surface as a clear error, not the vetoed `new SitemapData([], 0, [])`.
        var repo = new EmptyDiscoveredUrlsRepo();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SiteAnalyzerStepRelationalLoader.LoadSitemapAsync(
                repo,
                Guid.NewGuid(),
                [],
                CancellationToken.None));
    }

    [Fact]
    public async Task LoadSitemapAsync_uses_relational_sitemap_urls()
    {
        var profileId = Guid.NewGuid();
        var repo = new EmptyDiscoveredUrlsRepo(
        [
            new SiteAnalysisProfileDiscoveredUrlRow(
                Guid.NewGuid(),
                profileId,
                "https://example.com/a",
                "sitemap",
                DateTimeOffset.UtcNow),
            new SiteAnalysisProfileDiscoveredUrlRow(
                Guid.NewGuid(),
                profileId,
                "https://example.com/b",
                "crawl",
                DateTimeOffset.UtcNow),
        ]);

        var sitemap = await SiteAnalyzerStepRelationalLoader.LoadSitemapAsync(
            repo,
            profileId,
            [],
            CancellationToken.None);

        Assert.Equal(["https://example.com/a"], sitemap.SampleUrls);
        Assert.Equal(1, sitemap.TotalUrlsScanned);
    }

    [Fact]
    public async Task LoadSitemapAsync_includes_generated_sourcetype_rows()
    {
        // Step 1's own crawl-discovered rows (SourceType = "generated") must count as inventory,
        // not only rows sourced from a public /sitemap.xml.
        var profileId = Guid.NewGuid();
        var repo = new EmptyDiscoveredUrlsRepo(
        [
            new SiteAnalysisProfileDiscoveredUrlRow(
                Guid.NewGuid(),
                profileId,
                "https://example.com/",
                "generated",
                DateTimeOffset.UtcNow),
            new SiteAnalysisProfileDiscoveredUrlRow(
                Guid.NewGuid(),
                profileId,
                "https://example.com/about",
                "generated",
                DateTimeOffset.UtcNow),
        ]);

        var sitemap = await SiteAnalyzerStepRelationalLoader.LoadSitemapAsync(
            repo,
            profileId,
            [],
            CancellationToken.None);

        Assert.Equal(2, sitemap.SampleUrls.Count);
        Assert.Contains("https://example.com/about", sitemap.SampleUrls);
    }

    private sealed class EmptyDiscoveredUrlsRepo(IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>? urls = null)
        : ISiteAnalysisProfileRepository
    {
        public Task<Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(
            Guid profileId,
            CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>.Success(urls ?? []));

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
        public Task<Result> ReplaceNavigationLinksAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisProfileNavigationLinkWrite> links, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>> GetNavigationLinksAsync(
            Guid profileId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>.Success([]));
        public Task<Result> ReplaceHeadingsAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisProfileHeadingWrite> headings, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileHeadingRow>>> GetHeadingsAsync(
            Guid profileId, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result> ReplacePageSectionTreesAsync(
            Guid profileId, IReadOnlyList<SiteAnalysisPageSectionTreeWrite> pages, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>> GetPageSectionTreesAsync(
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
