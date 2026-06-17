using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentResearchWritingServiceTests
{
    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DocumentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ResearchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ProjectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task AttachResearchAsync_rejects_incomplete_research()
    {
        var research = CompletedResearch();
        research.Status = "running";

        var sut = new ContentResearchWritingService(
            new FakeDocumentService(ResearchDocument()),
            new FakeUrlResearchService(research),
            new FakeAiWritingService());

        var result = await sut.AttachResearchAsync(
            UserId,
            DocumentId,
            new AttachUrlResearchRequest { UrlResearchId = ResearchId });

        Assert.False(result.IsSuccess);
        Assert.Contains("not complete", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DraftFromResearchAsync_requires_attached_research()
    {
        var doc = ResearchDocument();
        doc.UrlResearchId = null;

        var sut = new ContentResearchWritingService(
            new FakeDocumentService(doc),
            new FakeUrlResearchService(CompletedResearch()),
            new FakeAiWritingService());

        var result = await sut.DraftFromResearchAsync(UserId, DocumentId);

        Assert.False(result.IsSuccess);
        Assert.Contains("Attach page research", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DraftFromResearchAsync_persists_draft_from_research_context()
    {
        var research = CompletedResearch();
        var ai = new FakeAiWritingService();
        var documents = new FakeDocumentService(ResearchDocument());

        var sut = new ContentResearchWritingService(
            documents,
            new FakeUrlResearchService(research),
            ai);

        var result = await sut.DraftFromResearchAsync(UserId, DocumentId);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("<h1>Drafted from research</h1>", result.Value!.Content);
        Assert.True(ai.DraftCalled);
        Assert.Equal("widget repair", ai.LastDraftRequest?.Research.DerivedKeyword);
        Assert.Equal("<h1>Drafted from research</h1>", documents.LastSavedHtml);
        Assert.Equal("widget repair", documents.LastSavedKeyword);
    }

    private static SeoContentDocument ResearchDocument() => new()
    {
        Id = DocumentId,
        ProjectId = ProjectId,
        UserId = UserId,
        Title = "Widget repair",
        TargetKeyword = "widget repair",
        TargetLocation = "United States",
        ContentHtml = "<h1>Widget repair</h1>",
        UrlResearchId = ResearchId,
    };

    private static SeoUrlResearch CompletedResearch() => new()
    {
        Id = ResearchId,
        ProjectId = ProjectId,
        UserId = UserId,
        SourceUrl = "https://example.com/widget-repair",
        DerivedKeyword = "widget repair",
        SearchLocation = "United States",
        Status = "completed",
        DataQuality = "full",
        IntentPrimary = "informational",
        IntentJustification = "how-to",
        PafType = "paragraph",
        PafFormat = "text",
        DirectAnswerInstruction = "Lead with a direct answer.",
        DominantContentFormat = "guide",
        MedianWordCountTop5 = 1500,
        MedianTitleLengthTop10 = 55,
        MedianH2CountTop5 = 4,
        ResearchedAt = DateTimeOffset.UtcNow,
    };

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

        public Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUrlResearchService(SeoUrlResearch research) : IUrlResearchService
    {
        public Task<Result<SeoUrlResearch>> GetFullAsync(Guid userId, Guid urlResearchId, CancellationToken ct = default) =>
            urlResearchId == research.Id
                ? Task.FromResult(Result<SeoUrlResearch>.Success(research))
                : Task.FromResult(Result<SeoUrlResearch>.NotFound("not found"));

        public Task<Result<SeoUrlResearch>> CreateQueuedAsync(
            Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(
            Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> PersistFullAsync(
            Guid userId, Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> UpdateStatusAsync(
            Guid userId, Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default) =>
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
