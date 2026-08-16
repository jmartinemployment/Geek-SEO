using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerServiceCanonicalProfileTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProfileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task EnqueueAsync_reuses_existing_complete_profile_for_reanalysis()
    {
        var repo = new FakeSiteAnalysisProfileRepository(new SiteAnalysisProfile
        {
            Id = ProfileId,
            ProjectId = ProjectId,
            Domain = "https://example.com",
            Status = "complete",
            AnalysisStepLog = "[]",
            StepStatusesJson = "{}",
        });
        var projects = new FakeProjectRepository(new SeoProject
        {
            Id = ProjectId,
            UserId = UserId,
            Name = "Example",
            Url = "https://example.com",
        });
        var sut = CreateSut(repo, projects);

        var id = await sut.EnqueueAsync(UserId, ProjectId, "https://example.com");

        Assert.Equal(ProfileId, id);
        Assert.False(repo.CreateCalled);
        Assert.Equal("pending", repo.LastStatus);
    }

    [Fact]
    public async Task EnqueueAsync_resets_existing_queued_profile_to_pending()
    {
        var repo = new FakeSiteAnalysisProfileRepository(new SiteAnalysisProfile
        {
            Id = ProfileId,
            ProjectId = ProjectId,
            Domain = "https://example.com",
            Status = "queued",
            AnalysisStepLog = "[]",
            StepStatusesJson = "{}",
        });
        var projects = new FakeProjectRepository(new SeoProject
        {
            Id = ProjectId,
            UserId = UserId,
            Name = "Example",
            Url = "https://example.com",
        });
        var sut = CreateSut(repo, projects);

        var id = await sut.EnqueueAsync(UserId, ProjectId, "https://example.com");

        Assert.Equal(ProfileId, id);
        Assert.False(repo.CreateCalled);
        Assert.Equal("pending", repo.LastStatus);
        Assert.True(repo.UpdateStatusCalls > 0);
    }

    private static SiteAnalyzerService CreateSut(
        ISiteAnalysisProfileRepository profiles,
        IProjectRepository projects) =>
        new(
            profiles,
            projects,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new SiteAnalysisProgressNotifier(
                NullHubContext.Instance,
                NullLogger<SiteAnalysisProgressNotifier>.Instance),
            null!,
            null!,
            NullLogger<SiteAnalyzerService>.Instance);

    private sealed class FakeProjectRepository(SeoProject project) : IProjectRepository
    {
        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoProject>>.Success([project]));

        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoProject>.Success(project));

        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoProject>.Success(project));

        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSiteAnalysisProfileRepository(SiteAnalysisProfile? latest) : ISiteAnalysisProfileRepository
    {
        public bool CreateCalled { get; private set; }
        public int UpdateStatusCalls { get; private set; }
        public string? LastStatus { get; private set; }

        public Task<Result<SiteAnalysisProfile>> CreateAsync(SiteAnalysisProfile profile, CancellationToken ct = default)
        {
            CreateCalled = true;
            return Task.FromResult(Result<SiteAnalysisProfile>.Success(profile));
        }

        public Task<Result<SiteAnalysisProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SiteAnalysisProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SiteAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(Guid profileId, bool includeFusion, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SiteAnalysisProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<SiteAnalysisProfile?>.Success(latest));
        public Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> GetHistoryAsync(Guid projectId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpsertStepRunAsync(Guid profileId, SiteAnalysisProfileStepRunUpsert stepRun, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
        public Task<Result> UpdateStepRunStatusAsync(Guid profileId, string stepSlug, SiteAnalysisProfileStepRunStatusPatch patch, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileStepRunRow>>> GetStepRunsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceSchemaSignalsAsync(Guid profileId, IReadOnlyList<SiteAnalysisProfileSchemaSignalWrite> signals, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileSchemaSignalRow>>> GetSchemaSignalsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceDiscoveredUrlsAsync(Guid profileId, IReadOnlyList<SiteAnalysisProfileDiscoveredUrlWrite> urls, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileDiscoveredUrlRow>>> GetDiscoveredUrlsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceNavigationLinksAsync(Guid profileId, IReadOnlyList<SiteAnalysisProfileNavigationLinkWrite> links, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileNavigationLinkRow>>> GetNavigationLinksAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceHeadingsAsync(Guid profileId, IReadOnlyList<SiteAnalysisProfileHeadingWrite> headings, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileHeadingRow>>> GetHeadingsAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplacePageSectionTreesAsync(Guid profileId, IReadOnlyList<SiteAnalysisPageSectionTreeWrite> pages, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisPageSectionTreeRow>>> GetPageSectionTreesAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplaceTopicCandidateEvidenceAsync(Guid profileId, IReadOnlyList<SiteAnalysisTopicCandidateEvidenceWrite> evidence, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisTopicCandidateEvidenceRow>>> GetTopicCandidateEvidenceAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> ReplacePageContentAsync(Guid profileId, SiteAnalysisProfilePageContentWrite content, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<SiteAnalysisProfilePageContentRow?>> GetPageContentAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult(Result<SiteAnalysisProfilePageContentRow?>.Success(null));
        public Task<Result> ReplaceSiteStructureAsync(Guid profileId, SiteAnalysisProfileSiteStructureWrite structure, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<SiteAnalysisProfileSiteStructureRow?>> GetSiteStructureAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult(Result<SiteAnalysisProfileSiteStructureRow?>.Success(null));

        public Task<Result> UpdateStatusAsync(Guid profileId, string status, string? step = null, int stepNumber = 0, int totalSteps = 0, string? errorMessage = null, SiteAnalysisStepLogEntry? stepLogEntry = null, CancellationToken ct = default)
        {
            UpdateStatusCalls++;
            LastStatus = status;
            return Task.FromResult(Result.Success());
        }

        public Task<Result> UpdateScoresAsync(Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateProfileSummaryAsync(Guid profileId, SiteAnalysisProfileSummaryPatch summary, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveFusionSnapshotAsync(Guid profileId, string fusionSnapshotJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdatePhaseStatusAsync(Guid profileId, SiteAnalysisPhaseStatusPatch patch, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> BulkUpsertTopicCandidatesAsync(Guid profileId, IReadOnlyList<SiteAnalysisTopicCandidateBulkUpsert> candidates, string idempotencyKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<SiteAnalysisTopicCandidateListResult>> GetTopicCandidatesAsync(Guid profileId, int page, int pageSize, bool? selectedOnly, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveAnalysisResultsAsync(Guid profileId, SiteAnalysisSaveRequest results, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarsAsync(IEnumerable<SiteAnalysisPillar> pillars, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertSubtopicsAsync(IEnumerable<SiteAnalysisSubtopic> subtopics, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertCompetitorsAsync(IEnumerable<SiteAnalysisCompetitor> competitors, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisCompetitor>>> GetCompetitorsAsync(Guid profileId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SiteAnalysisCompetitor>>.Success(Array.Empty<SiteAnalysisCompetitor>()));
        public Task<Result> UpdateCompetitorInsightsAsync(SiteAnalysisCompetitor competitor, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertEntitiesAsync(IEnumerable<SiteAnalysisEntity> entities, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarPagesAsync(IEnumerable<SiteAnalysisPillarPage> pages, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<SiteAnalysisQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateStepStatusAsync(Guid profileId, string slug, string status, SiteAnalysisStepLogEntry? entry = null, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> InvalidateDownstreamStepsAsync(Guid profileId, IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(Guid profileId, CancellationToken ct = default) =>
            Task.FromResult<Result<IReadOnlyDictionary<string, string>>>(
                Result<IReadOnlyDictionary<string, string>>.Success(new Dictionary<string, string>()));
        public Task<Result> ReplaceExtractedToolsAsync(Guid profileId, IReadOnlyList<SiteAnalysisProfileExtractedToolWrite> tools, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<IReadOnlyList<SiteAnalysisProfileExtractedToolRow>>> GetExtractedToolsAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult(Result<IReadOnlyList<SiteAnalysisProfileExtractedToolRow>>.Success([]));
    }

    private sealed class NullHubContext : Microsoft.AspNetCore.SignalR.IHubContext<GeekSeoBackend.Hubs.SeoRealtimeHub>
    {
        public static NullHubContext Instance { get; } = new();
        public Microsoft.AspNetCore.SignalR.IHubClients Clients => throw new NotSupportedException();
        public Microsoft.AspNetCore.SignalR.IGroupManager Groups => throw new NotSupportedException();
    }
}
