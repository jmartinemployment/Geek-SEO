using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentBodyLinkServiceTests
{
    private const string SampleHtml = """
        <h2 id="implementation">Implementation approach</h2>
        <p>Teams often compare free AI tools for market research before committing.</p>
        """;

    [Fact]
    public async Task ApplyAsync_persists_updated_html_when_instructions_match()
    {
        var doc = new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ContentHtml = SampleHtml,
            TargetKeyword = "AI market research",
            DocumentKind = ContentDocumentKinds.Pillar,
        };
        var documents = new FakeDocumentService(doc);
        var service = new ContentBodyLinkService(documents, new EmptyDocumentRepository(), new NoOpMigrator());

        var result = await service.ApplyAsync(
            Guid.NewGuid(),
            doc.Id,
            new ApplyBodyLinksRequest
            {
                Instructions =
                [
                    new BodyLinkInsertionInstruction
                    {
                        LinkId = "body-01",
                        TargetHeadingId = "implementation",
                        PlacementStrategy = BodyLinkPlacementStrategy.SectionFooter,
                        TargetPath = "/blog/best-ai-tools-market-research",
                        AnchorText = "best AI tools for market research",
                        IsTargetActive = true,
                    },
                ],
            });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Changed);
        Assert.Equal(1, result.Value.AppliedCount);
        Assert.Contains("related-guide-box", doc.ContentHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsync_uses_saved_plan_when_instructions_are_empty()
    {
        var pillar = PillarWithBodyPlan();
        var child = new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = pillar.ProjectId,
            ParentDocumentId = pillar.Id,
            PublishSlug = "best-ai-tools-market-research",
            Status = SpokeLinkStatuses.BodyGenerated,
            WordCount = 500,
            ContentHtml = "<p>Generated spoke body.</p>",
        };
        var repo = new BodyLinkDocumentRepository(pillar, child);
        var service = new ContentBodyLinkService(
            new FakeDocumentService(pillar, repo),
            repo,
            new NoOpMigrator());

        var result = await service.ApplyAsync(pillar.UserId, pillar.Id, new ApplyBodyLinksRequest());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Changed);
        Assert.Equal(1, result.Value.AppliedCount);
        Assert.Contains("href=\"/blog/best-ai-tools-market-research\"", pillar.ContentHtml, StringComparison.Ordinal);
    }

    private static SeoContentDocument PillarWithBodyPlan()
    {
        var plan = new ContentLinkPlan
        {
            BodyLinks =
            [
                new ContentLinkBodySlot
                {
                    InsertAfterH2Hint = "implementation",
                    TargetPath = "/blog/best-ai-tools-market-research",
                    AnchorText = "best AI tools for market research",
                    Priority = 1,
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
            ContentHtml = SampleHtml,
            LinkPlanJson = ContentLinkPlanJson.Serialize(plan),
            DocumentKind = ContentDocumentKinds.Pillar,
            Status = "writing",
        };
    }

    private sealed class FakeDocumentService(
        SeoContentDocument document,
        BodyLinkDocumentRepository? repo = null) : IContentDocumentService
    {
        public Task<Result<SeoContentDocument>> EnsureAccessAsync(
            Guid userId, Guid documentId, CancellationToken ct = default) =>
            documentId == document.Id
                ? Task.FromResult(Result<SeoContentDocument>.Success(document))
                : Task.FromResult(Result<SeoContentDocument>.Failure("not found"));

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid userId, Guid documentId, UpdateContentRequest request, CancellationToken ct = default)
        {
            document.ContentHtml = request.ContentHtml;
            if (repo is not null)
            {
                return repo.UpdateContentAsync(documentId, request, document.WordCount, ct);
            }

            return Task.FromResult(Result<SeoContentDocument>.Success(document));
        }

        public Task<Result<SeoContentDocument>> GetAsync(
            Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<IReadOnlyList<SeoContentDocument>>> ListByProjectAsync(
            Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
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

        public Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class EmptyDocumentRepository : IContentDocumentRepository
    {
        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoContentDocument>>.Success(Array.Empty<SeoContentDocument>()));

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid documentId, UpdateContentRequest request, int wordCount, CancellationToken ct = default) =>
            throw new NotImplementedException();

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

    private sealed class BodyLinkDocumentRepository(SeoContentDocument pillar, SeoContentDocument child)
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
}
