using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentSpokeServiceTests
{
    [Fact]
    public async Task CreateAsync_creates_child_with_parent_research_context()
    {
        var pillar = PillarDocument();
        var repo = new TrackingDocumentRepository(pillar);
        var service = new ContentSpokeService(new FakeDocumentService(pillar, repo), repo);

        var result = await service.CreateAsync(
            pillar.UserId,
            pillar.Id,
            new CreateContentSpokeRequest
            {
                Phrase = "best ai for market analysis",
                SourceType = SpokeSourceTypes.Pasf,
                PublishSlug = "best-ai-for-market-analysis",
            });

        Assert.True(result.IsSuccess);
        Assert.Equal("best-ai-for-market-analysis", result.Value!.PublishSlug);
        Assert.Single(repo.Documents.Where(d => d.ParentDocumentId == pillar.Id));
        var child = repo.Documents.Single(d => d.ParentDocumentId == pillar.Id);
        Assert.Equal(pillar.AnalysisRunId, child.AnalysisRunId);
        Assert.Equal(pillar.KeywordBundleJson, child.KeywordBundleJson);
        Assert.Equal(ContentDocumentKinds.Spoke, child.DocumentKind);
        Assert.Contains("Spoke draft shell", child.ContentHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_phrase_on_same_pillar()
    {
        var pillar = PillarDocument();
        var repo = new TrackingDocumentRepository(pillar);
        var service = new ContentSpokeService(new FakeDocumentService(pillar, repo), repo);
        var request = new CreateContentSpokeRequest
        {
            Phrase = "ai market research companies",
            SourceType = SpokeSourceTypes.Pasf,
        };

        Assert.True((await service.CreateAsync(pillar.UserId, pillar.Id, request)).IsSuccess);
        var duplicate = await service.CreateAsync(pillar.UserId, pillar.Id, request);

        Assert.False(duplicate.IsSuccess);
        Assert.Contains("already exists", duplicate.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListAsync_returns_only_children_of_pillar()
    {
        var pillar = PillarDocument();
        var repo = new TrackingDocumentRepository(pillar);
        var service = new ContentSpokeService(new FakeDocumentService(pillar, repo), repo);

        await service.CreateAsync(
            pillar.UserId,
            pillar.Id,
            new CreateContentSpokeRequest { Phrase = "ai market research report", SourceType = SpokeSourceTypes.Pasf });

        var otherChild = new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = pillar.ProjectId,
            UserId = pillar.UserId,
            ParentDocumentId = Guid.NewGuid(),
            DocumentKind = ContentDocumentKinds.Spoke,
            Title = "Other pillar spoke",
            TargetKeyword = "other",
            Status = "planned",
        };
        repo.Documents.Add(otherChild);

        var result = await service.ListAsync(pillar.UserId, pillar.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("ai market research report", result.Value![0].SpokeSourcePhrase);
    }

    private static SeoContentDocument PillarDocument() =>
        AnalysisRunTestData.FrozenResearchDocument(
            AnalysisRunTestData.MarketResearchPasfExport(),
            "ai market research tools");

    private sealed class FakeDocumentService(
        SeoContentDocument pillar,
        TrackingDocumentRepository repo) : IContentDocumentService
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

    private sealed class TrackingDocumentRepository(SeoContentDocument pillar) : IContentDocumentRepository
    {
        public List<SeoContentDocument> Documents { get; } = [pillar];

        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            Documents.FirstOrDefault(d => d.Id == documentId) is { } doc
                ? Task.FromResult(Result<SeoContentDocument>.Success(doc))
                : Task.FromResult(Result<SeoContentDocument>.NotFound("not found"));

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoContentDocument>>.Success(
                Documents.Where(d => d.ProjectId == projectId).ToList()));

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default)
        {
            var doc = new SeoContentDocument
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                UserId = userId,
                ParentDocumentId = request.ParentDocumentId,
                DocumentKind = ContentDocumentKindResolver.Resolve(request.DocumentKind, request.ParentDocumentId),
                PublishSlug = request.PublishSlug,
                SpokeSourceType = request.SpokeSourceType,
                SpokeSourcePhrase = request.SpokeSourcePhrase,
                Title = request.Title,
                TargetKeyword = request.TargetKeyword,
                TargetLocation = request.TargetLocation,
                AnalysisRunId = request.AnalysisRunId,
                SerpKeyword = request.SerpKeyword,
                SiteProfileId = request.SiteProfileId,
                SiteFocusJson = request.SiteFocusJson,
                SiteFocusCapturedAt = request.SiteFocusCapturedAt,
                KeywordBundleJson = request.KeywordBundleJson,
                KeywordBundleCapturedAt = request.KeywordBundleCapturedAt,
                Status = "planned",
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            Documents.Add(doc);
            return Task.FromResult(Result<SeoContentDocument>.Success(doc));
        }

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid documentId, UpdateContentRequest request, int wordCount, CancellationToken ct = default)
        {
            var doc = Documents.First(d => d.Id == documentId);
            doc.ContentHtml = request.ContentHtml;
            doc.Title = request.Title ?? doc.Title;
            doc.TargetKeyword = request.TargetKeyword ?? doc.TargetKeyword;
            doc.WordCount = wordCount;
            doc.UpdatedAt = DateTimeOffset.UtcNow;
            return Task.FromResult(Result<SeoContentDocument>.Success(doc));
        }

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

        public Task<Result> UpdateScoreAsync(
            Guid documentId, int score, string scoreComponentsJson, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> UpdateAiDetectionScoreAsync(
            Guid documentId, decimal score, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteAsync(Guid documentId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }
}
