using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeekSeoBackend.Tests;

public sealed class NicheAnalyzerServiceCanonicalProfileTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProfileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task EnqueueAsync_reuses_existing_complete_profile_for_reanalysis()
    {
        var repo = new FakeNicheProfileRepository(new NicheProfile
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
        var repo = new FakeNicheProfileRepository(new NicheProfile
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

    private static NicheAnalyzerService CreateSut(
        INicheProfileRepository profiles,
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
            NullHubContext.Instance,
            null!,
            NullLogger<NicheAnalyzerService>.Instance);

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

    private sealed class FakeNicheProfileRepository(NicheProfile? latest) : INicheProfileRepository
    {
        public bool CreateCalled { get; private set; }
        public int UpdateStatusCalls { get; private set; }
        public string? LastStatus { get; private set; }

        public Task<Result<NicheProfile>> CreateAsync(NicheProfile profile, CancellationToken ct = default)
        {
            CreateCalled = true;
            return Task.FromResult(Result<NicheProfile>.Success(profile));
        }

        public Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<Guid?>> GetProjectIdAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfileStatusRow?>> GetStatusRowAsync(Guid profileId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheAnalysisDetailsRow?>> GetAnalysisDetailsRowAsync(Guid profileId, bool includeFusion, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<NicheProfile?>.Success(latest));
        public Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(Guid projectId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpsertStepRunAsync(Guid profileId, NicheProfileStepRunUpsert stepRun, CancellationToken ct = default) =>
            Task.FromResult(Result.Success());
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
        public Task<Result> ReplacePageContentAsync(Guid profileId, NicheProfilePageContentWrite content, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<NicheProfilePageContentRow?>> GetPageContentAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult(Result<NicheProfilePageContentRow?>.Success(null));
        public Task<Result> ReplaceSiteStructureAsync(Guid profileId, NicheProfileSiteStructureWrite structure, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<NicheProfileSiteStructureRow?>> GetSiteStructureAsync(Guid profileId, CancellationToken ct = default) => Task.FromResult(Result<NicheProfileSiteStructureRow?>.Success(null));

        public Task<Result> UpdateStatusAsync(Guid profileId, string status, string? step = null, int stepNumber = 0, int totalSteps = 0, string? errorMessage = null, NicheAnalysisStepLogEntry? stepLogEntry = null, CancellationToken ct = default)
        {
            UpdateStatusCalls++;
            LastStatus = status;
            return Task.FromResult(Result.Success());
        }

        public Task<Result> UpdateScoresAsync(Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateProfileSummaryAsync(Guid profileId, NicheProfileSummaryPatch summary, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveFusionSnapshotAsync(Guid profileId, string fusionSnapshotJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdatePhaseStatusAsync(Guid profileId, NichePhaseStatusPatch patch, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> BulkUpsertTopicCandidatesAsync(Guid profileId, IReadOnlyList<NicheTopicCandidateBulkUpsert> candidates, string idempotencyKey, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<NicheTopicCandidateListResult>> GetTopicCandidatesAsync(Guid profileId, int page, int pageSize, bool? selectedOnly, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> SaveAnalysisResultsAsync(Guid profileId, NicheAnalysisSaveRequest results, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertCompetitorsAsync(IEnumerable<NicheCompetitor> competitors, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateCompetitorInsightsAsync(NicheCompetitor competitor, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<NicheQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result<int>> FailStaleProcessingAsync(TimeSpan maxAge, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Result> UpdateStepStatusAsync(Guid profileId, string slug, string status, NicheAnalysisStepLogEntry? entry = null, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> InvalidateDownstreamStepsAsync(Guid profileId, IReadOnlyList<string> downstreamSlugs, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> UpdateCrawledUrlsAsync(Guid profileId, string crawledUrlsJson, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result<IReadOnlyDictionary<string, string>>> GetStepStatusesAsync(Guid profileId, CancellationToken ct = default) =>
            Task.FromResult<Result<IReadOnlyDictionary<string, string>>>(
                Result<IReadOnlyDictionary<string, string>>.Success(new Dictionary<string, string>()));
    }

    private sealed class NullHubContext : Microsoft.AspNetCore.SignalR.IHubContext<GeekSeoBackend.Hubs.SeoContentScoringHub>
    {
        public static NullHubContext Instance { get; } = new();
        public Microsoft.AspNetCore.SignalR.IHubClients Clients => throw new NotSupportedException();
        public Microsoft.AspNetCore.SignalR.IGroupManager Groups => throw new NotSupportedException();
    }
}
