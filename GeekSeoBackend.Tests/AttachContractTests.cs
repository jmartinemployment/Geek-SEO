using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

using static GeekSeoBackend.Tests.AnalysisRunTestData;

namespace GeekSeoBackend.Tests;

public sealed class AttachContractTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectId = AnalysisRunTestData.ProjectId;
    private static readonly Guid RunId = AnalysisRunTestData.RunId;

    [Fact]
    public void ValidateAnalysisRunExport_rejects_failed_status()
    {
        var export = AnalysisRunTestData.CompletedExport() with { Status = "Failed" };

        var result = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);

        Assert.False(result.IsSuccess);
        Assert.Contains("failed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnalysisRunExport_rejects_without_organic_results()
    {
        var export = AnalysisRunTestData.CompletedExport() with { Serp = [] };

        var result = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);

        Assert.False(result.IsSuccess);
        Assert.Contains("organic", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnalysisRunExport_rejects_without_gap_topics()
    {
        var export = AnalysisRunTestData.CompletedExport() with { GapTopics = [] };

        var result = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);

        Assert.False(result.IsSuccess);
        Assert.Contains("gap topics", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnalysisRunExport_rejects_without_source_headings()
    {
        var export = AnalysisRunTestData.CompletedExport() with { SourceHeadings = [] };

        var result = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);

        Assert.False(result.IsSuccess);
        Assert.Contains("headings", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnalysisRunExport_rejects_without_competitor_headings()
    {
        var export = AnalysisRunTestData.CompletedExport() with { Competitors = [] };

        var result = ResearchBackedWriteGate.ValidateAnalysisRunExport(export);

        Assert.False(result.IsSuccess);
        Assert.Contains("competitor", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAnalysisRunExport_accepts_full_research_pack()
    {
        var result = ResearchBackedWriteGate.ValidateAnalysisRunExport(AnalysisRunTestData.CompletedExport());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateAsync_rejects_analysis_run_without_organic_serp()
    {
        var export = AnalysisRunTestData.CompletedExport() with { Serp = [] };
        var projects = new FakeProjectRepository(ProjectId);
        var sut = new ContentDocumentService(
            new FakeContentDocumentRepository(),
            projects,
            CreateHandoffService(export));

        var result = await sut.CreateAsync(UserId, new CreateContentDocumentRequest
        {
            ProjectId = Guid.Empty,
            AnalysisRunId = RunId,
            SiteProfileId = SiteProfileId,
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("organic", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_preserves_explicit_target_keyword_over_export()
    {
        var repo = new FakeContentDocumentRepository();
        var export = AnalysisRunTestData.CompletedExport() with { Keyword = "widget repair" };
        var projects = new FakeProjectRepository(ProjectId);
        var sut = new ContentDocumentService(
            repo,
            projects,
            CreateHandoffService(export));

        var result = await sut.CreateAsync(UserId, new CreateContentDocumentRequest
        {
            ProjectId = Guid.Empty,
            AnalysisRunId = RunId,
            SiteProfileId = SiteProfileId,
            TargetKeyword = "emergency widget repair cost",
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(repo.LastCreateRequest);
        Assert.Equal("emergency widget repair cost", repo.LastCreateRequest.TargetKeyword);
        Assert.Equal("widget repair", repo.LastCreateRequest.SerpKeyword);
    }

    [Fact]
    public async Task CreateAsync_copies_keyword_from_analysis_run_export()
    {
        var repo = new FakeContentDocumentRepository();
        var export = AnalysisRunTestData.CompletedExport() with { Keyword = "custom widget repair" };
        var projects = new FakeProjectRepository(ProjectId);
        var sut = new ContentDocumentService(
            repo,
            projects,
            CreateHandoffService(export));

        var result = await sut.CreateAsync(UserId, new CreateContentDocumentRequest
        {
            ProjectId = Guid.Empty,
            AnalysisRunId = RunId,
            SiteProfileId = SiteProfileId,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(repo.LastCreateRequest);
        Assert.Equal("custom widget repair", repo.LastCreateRequest.TargetKeyword);
        Assert.Equal("custom widget repair", repo.LastCreateRequest.SerpKeyword);
    }

    [Fact]
    public async Task CreateAsync_resolves_geek_seo_project_from_site_profile_without_url_projectId()
    {
        var repo = new FakeContentDocumentRepository();
        var export = AnalysisRunTestData.CompletedExport();
        var projects = new FakeProjectRepository(ProjectId);
        var sut = new ContentDocumentService(
            repo,
            projects,
            CreateHandoffService(export));

        var result = await sut.CreateAsync(UserId, new CreateContentDocumentRequest
        {
            ProjectId = Guid.Empty,
            AnalysisRunId = RunId,
            SiteProfileId = SiteProfileId,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(repo.LastCreateRequest);
        Assert.Equal(ProjectId, repo.LastCreateRequest.ProjectId);
    }


    [Fact]
    public async Task CreateAsync_accepts_analysisRunId_without_siteProfileId()
    {
        var repo = new FakeContentDocumentRepository();
        var export = AnalysisRunTestData.CompletedExport();
        var sut = new ContentDocumentService(
            repo,
            new FakeProjectRepository(ProjectId),
            CreateHandoffService(export));

        var result = await sut.CreateAsync(UserId, new CreateContentDocumentRequest
        {
            AnalysisRunId = RunId,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(repo.LastCreateRequest);
        Assert.Equal(ProjectId, repo.LastCreateRequest.ProjectId);
    }

    private sealed class FakeProjectRepository(Guid projectId) : IProjectRepository
    {
        public Task<Result<SeoProject>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            id == projectId
                ? Task.FromResult(Result<SeoProject>.Success(new SeoProject
                {
                    Id = projectId,
                    UserId = UserId,
                    Name = "Test",
                    Url = "https://example.com",
                    DefaultLocation = "United States",
                }))
                : Task.FromResult(Result<SeoProject>.NotFound("not found"));

        public Task<Result<SeoProject>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
            GetByIdAsync(id, ct);

        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoProject>>.Success(
                (IReadOnlyList<SeoProject>)
                [
                    new SeoProject
                    {
                        Id = projectId,
                        UserId = UserId,
                        Name = "Test",
                        Url = "https://example.com",
                        DefaultLocation = "United States",
                    }
                ]));

        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeContentDocumentRepository : IContentDocumentRepository
    {
        public CreateContentDocumentRequest? LastCreateRequest { get; private set; }

        public Task<Result<SeoContentDocument>> CreateAsync(Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default)
        {
            LastCreateRequest = request;
            return Task.FromResult(Result<SeoContentDocument>.Success(new SeoContentDocument
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                UserId = userId,
                Title = request.Title,
                TargetKeyword = request.TargetKeyword,
                TargetLocation = request.TargetLocation,
                AnalysisRunId = request.AnalysisRunId,
            }));
        }

        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateContentAsync(Guid documentId, UpdateContentRequest request, int wordCount, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(Guid documentId, string status, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> AttachAnalysisRunAsync(
            Guid documentId,
            Guid analysisRunId,
            string targetKeyword,
            string serpKeyword,
            Guid siteProfileId,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateFeaturedImageAsync(Guid documentId, string featuredImageUrl, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> UpdateScoreAsync(Guid documentId, int score, string scoreComponentsJson, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> UpdateAiDetectionScoreAsync(Guid documentId, decimal score, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

}
