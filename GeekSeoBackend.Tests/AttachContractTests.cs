using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class AttachContractTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProjectId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ResearchId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void ValidateResearchForProject_rejects_cross_project()
    {
        var research = CompletedResearch(OtherProjectId);

        var result = ResearchBackedWriteGate.ValidateResearchForProject(ProjectId, research);

        Assert.False(result.IsSuccess);
        Assert.Contains("different project", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateResearchForProject_rejects_incomplete_status()
    {
        var research = CompletedResearch(ProjectId);
        research.Status = "queued";

        var result = ResearchBackedWriteGate.ValidateResearchForProject(ProjectId, research);

        Assert.False(result.IsSuccess);
        Assert.Contains("not complete", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_rejects_urlResearchId_from_other_project()
    {
        var sut = new ContentDocumentService(
            new FakeContentDocumentRepository(),
            new FakeProjectRepository(ProjectId),
            new FakeUrlResearchService(CompletedResearch(OtherProjectId)));

        var result = await sut.CreateAsync(UserId, new CreateContentDocumentRequest
        {
            ProjectId = ProjectId,
            UrlResearchId = ResearchId,
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("different project", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_copies_keyword_from_completed_research()
    {
        var repo = new FakeContentDocumentRepository();
        var research = CompletedResearch(ProjectId);
        research.DerivedKeyword = "custom widget repair";
        research.SearchLocation = "Austin, TX";
        var sut = new ContentDocumentService(
            repo,
            new FakeProjectRepository(ProjectId),
            new FakeUrlResearchService(research));

        var result = await sut.CreateAsync(UserId, new CreateContentDocumentRequest
        {
            ProjectId = ProjectId,
            UrlResearchId = ResearchId,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(repo.LastCreateRequest);
        Assert.Equal("custom widget repair", repo.LastCreateRequest.TargetKeyword);
        Assert.Equal("Austin, TX", repo.LastCreateRequest.TargetLocation);
    }

    private static SeoUrlResearch CompletedResearch(Guid projectId) => new()
    {
        Id = ResearchId,
        ProjectId = projectId,
        UserId = UserId,
        SourceUrl = "https://example.com/page",
        DerivedKeyword = "widgets",
        SearchLocation = "United States",
        Status = "completed",
    };

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
                }))
                : Task.FromResult(Result<SeoProject>.NotFound("not found"));

        public Task<Result<SeoProject>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
            GetByIdAsync(id, ct);

        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUrlResearchService(SeoUrlResearch research) : IUrlResearchService
    {
        public Task<Result<SeoUrlResearch>> GetFullAsync(Guid userId, Guid urlResearchId, CancellationToken ct = default) =>
            urlResearchId == research.Id
                ? Task.FromResult(Result<SeoUrlResearch>.Success(research))
                : Task.FromResult(Result<SeoUrlResearch>.NotFound("not found"));

        public Task<Result<SeoUrlResearch>> CreateQueuedAsync(Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> PersistFullAsync(Guid userId, Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> UpdateStatusAsync(Guid userId, Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default) =>
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
                UrlResearchId = request.UrlResearchId,
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
