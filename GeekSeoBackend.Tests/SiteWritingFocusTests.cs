using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class SiteWritingFocusTests
{
    [Fact]
    public async Task Assembler_includes_project_and_business_summary()
    {
        var projectId = Guid.NewGuid();
        var projects = new StubProjectRepository(projectId, "Geek at Your Spot", "https://geekatyourspot.com");
        var assembler = new SiteWritingFocusAssembler(
            projects,
            new StubNicheProfileRepository(projectId, new NicheProfile
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                PrimaryNiche = "AI consulting",
                NicheDescription = "South Florida SMB AI adoption.",
                Pillars = [new NichePillar { PillarTopic = "AI automation", PrimaryKeyword = "business automation", DisplayOrder = 1 }],
            }),
            new ContentWritingTestFixtures.NullNicheAnalyticsRepository(),
            new StubSiteResearchRepository("Managed IT and AI consulting for South Florida businesses."));

        var focus = await assembler.AssembleLegacyAsync(
            Guid.NewGuid(),
            projectId,
            "ai chatbot for small business",
            "Miami, FL",
            "ai chatbot pricing");

        Assert.Equal("Geek at Your Spot", focus.SiteName);
        Assert.Equal("AI consulting", focus.PrimaryNiche);
        Assert.Contains("South Florida", focus.BusinessSummary);
        Assert.Equal("AI automation", focus.MatchedPillarTopic);
        Assert.Contains("ai chatbot", focus.WritingInstructions, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplySiteFocus_populates_business_context_from_snapshot()
    {
        var focus = new SiteWritingFocus
        {
            SiteName = "Geek at Your Spot",
            SiteUrl = "https://geekatyourspot.com",
            PrimaryNiche = "AI consulting",
            WritingInstructions = "Write for South Florida SMB owners adopting AI.",
        };
        var json = SiteWritingFocusSerializer.Serialize(focus);
        var document = new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SiteFocusJson = json,
        };
        var context = AnalysisRunTestData.MinimalWritingContext();

        var merged = WritingResearchContextLoader.ApplySiteFocus(context, focus);

        Assert.NotNull(merged.SiteFocus);
        Assert.Equal("Geek at Your Spot", merged.SiteFocus.SiteName);
        Assert.Contains("South Florida", merged.BusinessContext);
    }

    [Fact]
    public void BuildResearchDraftUserPrompt_includes_site_focus_block()
    {
        var context = AnalysisRunTestData.MinimalWritingContext() with
        {
            SiteFocus = new SiteWritingFocus
            {
                SiteName = "Geek at Your Spot",
                SiteUrl = "https://geekatyourspot.com",
                PrimaryNiche = "AI consulting",
                MatchedPillarTopic = "AI automation",
                GeoAnchorNodes = ["Miami-Dade County", "Broward County"],
            },
            BusinessContext = "Write for South Florida SMB owners adopting AI.",
        };

        var prompt = ArticlePromptBuilder.BuildResearchDraftUserPrompt(new ResearchDraftRequest
        {
            Research = context,
            Title = "AI chatbot guide",
            TargetWordCount = 1200,
        });

        Assert.Contains("Site writing focus:", prompt);
        Assert.Contains("Geek at Your Spot", prompt);
        Assert.Contains("AI automation", prompt);
        Assert.Contains("Miami-Dade County", prompt);
    }

    private sealed class StubProjectRepository(Guid projectId, string name, string url) : IProjectRepository
    {
        public Task<Result<SeoProject>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            id == projectId
                ? Task.FromResult(Result<SeoProject>.Success(new SeoProject
                {
                    Id = projectId,
                    UserId = Guid.NewGuid(),
                    Name = name,
                    Url = url,
                    DefaultLocation = "Miami, FL",
                    BusinessAddress = "Fort Lauderdale, FL",
                    LocalSeoEnabled = true,
                }))
                : Task.FromResult(Result<SeoProject>.NotFound("not found"));

        public Task<Result<SeoProject>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubNicheProfileRepository(Guid projectId, NicheProfile profile) : INicheProfileRepository
    {
        public Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid id, CancellationToken ct = default) =>
            id == projectId
                ? Task.FromResult(Result<NicheProfile?>.Success(profile))
                : Task.FromResult(Result<NicheProfile?>.Success(null));

        public Task<Result<NicheProfile>> CreateAsync(NicheProfile p, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(Guid profileId, bool includeFusion, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(Guid pid, CancellationToken ct = default) => throw new NotSupportedException();
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

    private sealed class StubSiteResearchRepository(string summary) : ISiteResearchRepository
    {
        public Task<Result<SeoSiteResearch>> GetOrCreateForProjectAsync(
            Guid userId, CreateSiteResearchRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoSiteResearch>.Success(new SeoSiteResearch
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                UserId = userId,
                SiteUrl = request.SiteUrl,
                BusinessSummary = summary,
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
