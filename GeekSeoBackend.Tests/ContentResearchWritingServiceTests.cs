using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

using static GeekSeoBackend.Tests.AnalysisRunTestData;

namespace GeekSeoBackend.Tests;

public sealed class ContentResearchWritingServiceTests
{
    [Fact]
    public async Task AttachResearchAsync_rejects_failed_analysis_run()
    {
        var export = CompletedExport() with { Status = "Failed" };
        var documents = new ContentDocumentService(
            new NoOpContentDocumentRepository(),
            new StubProjectRepository(),
            CreateHandoffService(export));
        var sut = new ContentResearchWritingService(
            documents,
            new FakeAiWritingService(),
            CreateContextLoader());

        var result = await sut.AttachResearchAsync(
            UserId,
            DocumentId,
            new AttachAnalysisRunRequest
            {
                AnalysisRunId = RunId,
                SiteProfileId = SiteProfileId,
            });

        Assert.False(result.IsSuccess);
        Assert.Contains("failed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AttachResearchAsync_accepts_analysisRunId_without_siteProfileId()
    {
        var sut = CreateSut(FrozenResearchDocument());

        var result = await sut.AttachResearchAsync(
            UserId,
            DocumentId,
            new AttachAnalysisRunRequest { AnalysisRunId = RunId });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DraftFromResearchAsync_requires_attached_analysis_run()
    {
        var doc = FrozenResearchDocument();
        doc.AnalysisRunId = null;
        doc.KeywordBundleJson = null;

        var sut = CreateSut(doc);

        var result = await sut.DraftFromResearchAsync(UserId, DocumentId);

        Assert.False(result.IsSuccess);
        Assert.Contains("Complete research in Site Analyzer", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DraftFromResearchAsync_uses_document_target_keyword_over_serp_keyword()
    {
        var export = CompletedExport() with { Keyword = "widget repair" };
        var doc = FrozenResearchDocument(export, "emergency widget repair");
        var ai = new FakeAiWritingService();
        var documents = new FakeDocumentService(doc);
        var sut = new ContentResearchWritingService(
            documents,
            ai,
            CreateContextLoader());

        var result = await sut.DraftFromResearchAsync(UserId, DocumentId);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("emergency widget repair", ai.LastDraftRequest?.Research.DerivedKeyword);
        Assert.Equal("widget repair", ai.LastDraftRequest?.Research.SerpKeyword);
        Assert.Equal("emergency widget repair", documents.LastSavedKeyword);
    }

    [Fact]
    public async Task DraftFromResearchAsync_persists_draft_from_frozen_bundle()
    {
        var ai = new FakeAiWritingService();
        var documents = new FakeDocumentService(FrozenResearchDocument());
        var sut = new ContentResearchWritingService(
            documents,
            ai,
            CreateContextLoader());

        var result = await sut.DraftFromResearchAsync(UserId, DocumentId);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("<h1>Drafted from research</h1>", result.Value!.Content);
        Assert.True(ai.DraftCalled);
        Assert.Equal("widget repair", ai.LastDraftRequest?.Research.DerivedKeyword);
        Assert.Equal("<h1>Drafted from research</h1>", documents.LastSavedHtml);
        Assert.Equal("widget repair", documents.LastSavedKeyword);
    }

    private static ContentResearchWritingService CreateSut(SeoContentDocument document) =>
        new(
            new FakeDocumentService(document),
            new FakeAiWritingService(),
            CreateContextLoader());

    private sealed class StubProjectRepository : IProjectRepository
    {
        public Task<Result<SeoProject>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            id == ProjectId
                ? Task.FromResult(Result<SeoProject>.Success(new SeoProject
                {
                    Id = ProjectId,
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

    private sealed class NoOpContentDocumentRepository : IContentDocumentRepository
    {
        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoContentDocument>.Success(FrozenResearchDocument()));

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> CreateAsync(Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
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

    private sealed class FakeDocumentService(SeoContentDocument document) : IContentDocumentService
    {
        public string? LastSavedHtml { get; private set; }
        public string? LastSavedKeyword { get; private set; }

        public Task<Result<SeoContentDocument>> EnsureAccessAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            documentId == document.Id
                ? Task.FromResult(Result<SeoContentDocument>.Success(document))
                : Task.FromResult(Result<SeoContentDocument>.NotFound("not found"));

        public Task<Result<SeoContentDocument>> GetAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            EnsureAccessAsync(userId, documentId, ct);

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid userId, Guid documentId, UpdateContentRequest request, CancellationToken ct = default)
        {
            LastSavedHtml = request.ContentHtml;
            LastSavedKeyword = request.TargetKeyword;
            document.ContentHtml = request.ContentHtml ?? document.ContentHtml;
            if (!string.IsNullOrWhiteSpace(request.TargetKeyword))
                document.TargetKeyword = request.TargetKeyword;
            return Task.FromResult(Result<SeoContentDocument>.Success(document));
        }

        public Task<Result<IReadOnlyList<SeoContentDocument>>> ListByProjectAsync(
            Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(
            Guid userId, Guid documentId, string status, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
            Guid userId, Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> AttachAnalysisRunAsync(
            Guid userId, Guid documentId, Guid analysisRunId, string targetKeyword, string serpKeyword, Guid? siteProfileId = null, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoContentDocument>.Success(document));

        public Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAiWritingService : IAIWritingService
    {
        public bool DraftCalled { get; private set; }
        public ResearchDraftRequest? LastDraftRequest { get; private set; }

        public Task<Result<BackgroundJobStatus>> EnqueueFullArticleAsync(
            Guid userId, FullArticleRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<BackgroundJobStatus>> EnqueueBulkArticlesAsync(
            Guid userId, BulkArticleRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<WritingTextResult>> GenerateOutlineAsync(
            Guid userId, WritingOutlineRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<WritingTextResult>> GenerateDraftAsync(
            Guid userId, WritingDraftRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<WritingTextResult>> GenerateDraftFromResearchAsync(
            Guid userId, ResearchDraftRequest request, CancellationToken ct = default)
        {
            DraftCalled = true;
            LastDraftRequest = request;
            return Task.FromResult(Result<WritingTextResult>.Success(
                new WritingTextResult { Content = "<h1>Drafted from research</h1>" }));
        }

        public Task<Result<WritingTextResult>> HumanizeAsync(
            Guid userId, HumanizeRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<AiDetectionResult>> DetectAsync(
            Guid userId, DetectAiRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
