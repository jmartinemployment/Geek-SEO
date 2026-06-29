using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentLinkedFaqServiceTests
{
    [Fact]
    public async Task GenerateLinkedFaqsAsync_writes_faq_section_with_registry_link()
    {
        var pillar = PillarWithPlan();
        var child = new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = pillar.ProjectId,
            UserId = pillar.UserId,
            ParentDocumentId = pillar.Id,
            PublishSlug = "best-ai-tools-market-research",
            Status = SpokeLinkStatuses.BodyGenerated,
            WordCount = 500,
            ContentHtml = "<p>Spoke body content here.</p>",
        };
        var repo = new LinkedFaqDocumentRepository(pillar, child);
        var service = new ContentLinkedFaqService(
            new FakeDocumentService(pillar, repo),
            repo,
            new NoOpMigrator(),
            new FakeAiProvider(
                """
                {"faqResults":[{"id":"faq-01","question":"Which AI tools are best for market research?","answerHtml":"A practical <a href=\"/blog/best-ai-tools-market-research\">best AI tools for market research</a> comparison helps teams avoid data-cap traps."}]}
                """));

        var result = await service.GenerateLinkedFaqsAsync(pillar.UserId, pillar.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.LinkedCount);
        Assert.Contains("Frequently Asked Questions", pillar.ContentHtml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/blog/best-ai-tools-market-research\"", pillar.ContentHtml, StringComparison.Ordinal);
    }

    private static SeoContentDocument PillarWithPlan()
    {
        var plan = new ContentLinkPlan
        {
            FaqItems =
            [
                new ContentLinkFaqItem
                {
                    Question = "Which AI tools are best for market research?",
                    TargetPath = "/blog/best-ai-tools-market-research",
                    AnchorText = "best AI tools for market research",
                },
            ],
        };

        return new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "AI market research tools",
            TargetKeyword = "AI market research tools",
            ContentHtml = "<p>" + new string('x', 220) + "</p>",
            LinkPlanJson = ContentLinkPlanJson.Serialize(plan),
            DocumentKind = ContentDocumentKinds.Pillar,
            Status = "writing",
        };
    }

    private sealed class FakeDocumentService(
        SeoContentDocument pillar,
        LinkedFaqDocumentRepository repo) : IContentDocumentService
    {
        public Task<Result<SeoContentDocument>> EnsureAccessAsync(
            Guid userId, Guid documentId, CancellationToken ct = default)
        {
            var doc = repo.Documents.FirstOrDefault(d => d.Id == documentId);
            return doc is not null && doc.UserId == userId
                ? Task.FromResult(Result<SeoContentDocument>.Success(doc))
                : Task.FromResult(Result<SeoContentDocument>.Failure("Access denied"));
        }

        public Task<Result<SeoContentDocument>> GetAsync(
            Guid userId, Guid documentId, CancellationToken ct = default) =>
            EnsureAccessAsync(userId, documentId, ct);

        public Task<Result<IReadOnlyList<SeoContentDocument>>> ListByProjectAsync(
            Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid userId, Guid documentId, UpdateContentRequest request, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(
            Guid userId, Guid documentId, string status, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
            Guid userId, Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> AttachAnalysisRunAsync(
            Guid userId,
            Guid documentId,
            Guid analysisRunId,
            string targetKeyword,
            string serpKeyword,
            Guid? siteProfileId = null,
            CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteAsync(
            Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class LinkedFaqDocumentRepository(SeoContentDocument pillar, SeoContentDocument child)
        : IContentDocumentRepository
    {
        public List<SeoContentDocument> Documents { get; } = [pillar, child];

        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            Documents.FirstOrDefault(d => d.Id == documentId) is { } doc
                ? Task.FromResult(Result<SeoContentDocument>.Success(doc))
                : Task.FromResult(Result<SeoContentDocument>.NotFound("not found"));

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoContentDocument>>.Success(
                Documents.Where(d => d.ProjectId == projectId).ToList()));

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid documentId, UpdateContentRequest request, int wordCount, CancellationToken ct = default)
        {
            var doc = Documents.First(d => d.Id == documentId);
            doc.ContentHtml = request.ContentHtml;
            doc.WordCount = wordCount;
            return Task.FromResult(Result<SeoContentDocument>.Success(doc));
        }

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(
            Guid documentId, string status, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
            Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> AttachAnalysisRunAsync(
            Guid documentId,
            Guid analysisRunId,
            string targetKeyword,
            string serpKeyword,
            Guid siteProfileId,
            CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> UpdateFeaturedImageAsync(
            Guid documentId, string featuredImageUrl, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> UpdateBlogSpokeAsync(
            Guid documentId, string blogSpokeJson, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> UpdateLinkPlanAsync(
            Guid documentId, string linkPlanJson, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> MigrateBlogSpokeChildIfAbsentAsync(
            Guid userId,
            Guid pillarDocumentId,
            MigrateBlogSpokeChildPayload payload,
            CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> UpdateScoreAsync(
            Guid documentId, int score, string scoreComponentsJson, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> UpdateAiDetectionScoreAsync(
            Guid documentId, decimal score, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteAsync(Guid documentId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class NoOpMigrator : IContentBlogSpokeMigrator
    {
        public Task<Result<Guid?>> EnsureMigratedChildAsync(
            Guid userId, Guid pillarDocumentId, CancellationToken ct = default) =>
            Task.FromResult(Result<Guid?>.Success(null));
    }

    private sealed class FakeAiProvider(string content) : IAIProvider
    {
        public string ProviderName => "fake";

        public Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<AIResponse>.Success(new AIResponse
            {
                Content = content,
                Model = "fake",
                InputTokens = 0,
                OutputTokens = 0,
                StopReason = "end_turn",
            }));
    }
}
