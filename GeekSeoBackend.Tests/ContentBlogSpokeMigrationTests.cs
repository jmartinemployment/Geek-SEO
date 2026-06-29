using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentBlogSpokeMigrationTests
{
    [Fact]
    public async Task EnsureMigratedChild_creates_child_from_blog_spoke_json()
    {
        var pillar = PillarWithBlogSpoke();
        var repo = new MigrationDocumentRepository(pillar);
        var migrator = new ContentBlogSpokeMigrator(new FakeDocumentService(pillar, repo), repo);

        var result = await migrator.EnsureMigratedChildAsync(pillar.UserId, pillar.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var child = repo.Documents.Single(d => d.ParentDocumentId == pillar.Id);
        Assert.Equal(ContentDocumentKinds.Spoke, child.DocumentKind);
        Assert.Equal(SpokeSourceTypes.Migrated, child.SpokeSourceType);
        Assert.Equal(SpokeLinkStatuses.BodyGenerated, child.Status);
        Assert.Equal("what-ai-content-tooling-costs", child.PublishSlug);
        Assert.Contains("pricing breakdown", child.ContentHtml, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(pillar.BlogSpokeJson);
    }

    [Fact]
    public async Task EnsureMigratedChild_returns_existing_child_on_second_call()
    {
        var pillar = PillarWithBlogSpoke();
        var repo = new MigrationDocumentRepository(pillar);
        var migrator = new ContentBlogSpokeMigrator(new FakeDocumentService(pillar, repo), repo);

        var first = await migrator.EnsureMigratedChildAsync(pillar.UserId, pillar.Id);
        var second = await migrator.EnsureMigratedChildAsync(pillar.UserId, pillar.Id);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
        Assert.Single(repo.Documents.Where(d => d.ParentDocumentId == pillar.Id));
    }

    [Fact]
    public async Task EnsureMigratedChild_skips_when_no_blog_spoke_json()
    {
        var pillar = PillarWithBlogSpoke();
        pillar.BlogSpokeJson = null;
        var repo = new MigrationDocumentRepository(pillar);
        var migrator = new ContentBlogSpokeMigrator(new FakeDocumentService(pillar, repo), repo);

        var result = await migrator.EnsureMigratedChildAsync(pillar.UserId, pillar.Id);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.DoesNotContain(repo.Documents, d => d.ParentDocumentId == pillar.Id);
    }

  [Fact]
    public async Task GetAsync_returns_cluster_document_id_after_migration()
    {
        var pillar = PillarWithBlogSpoke();
        var repo = new MigrationDocumentRepository(pillar);
        var documents = new FakeDocumentService(pillar, repo);
        var migrator = new ContentBlogSpokeMigrator(documents, repo);
        var service = new ContentBlogSpokeService(documents, repo, migrator, new NoOpAiProvider());

        var result = await service.GetAsync(pillar.UserId, pillar.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.ClusterDocumentId);
        Assert.Equal("What AI content tooling costs", result.Value.Spoke.Title);
    }

    private static SeoContentDocument PillarWithBlogSpoke()
    {
        var spoke = new ContentBlogSpoke
        {
            Title = "What AI content tooling costs",
            Slug = "what-ai-content-tooling-costs",
            PrimaryKeyword = "what AI content tooling actually costs",
            SpokeType = "cost",
            ContentHtml = "<h2>Pricing breakdown</h2><p>" + string.Join(' ', Enumerable.Repeat("detail", 60)) + "</p>",
            Excerpt = "A pricing guide.",
            MetaDescription = "Learn what AI content tooling costs.",
        };

        return new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "AI content operations",
            TargetKeyword = "AI content operations",
            ContentHtml = "<p>" + new string('x', 220) + "</p>",
            BlogSpokeJson = JsonSerializer.Serialize(spoke),
            DocumentKind = ContentDocumentKinds.Pillar,
            Status = "writing",
        };
    }

    private sealed class FakeDocumentService(
        SeoContentDocument pillar,
        MigrationDocumentRepository repo) : IContentDocumentService
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

    private sealed class MigrationDocumentRepository(SeoContentDocument pillar) : IContentDocumentRepository
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
                Status = "planned",
                CreatedAt = DateTimeOffset.UtcNow,
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
            doc.WordCount = wordCount;
            if (request.Title is not null) doc.Title = request.Title;
            if (request.TargetKeyword is not null) doc.TargetKeyword = request.TargetKeyword;
            doc.UpdatedAt = DateTimeOffset.UtcNow;
            return Task.FromResult(Result<SeoContentDocument>.Success(doc));
        }

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(
            Guid documentId, string status, CancellationToken ct = default)
        {
            var doc = Documents.First(d => d.Id == documentId);
            doc.Status = status;
            doc.UpdatedAt = DateTimeOffset.UtcNow;
            return Task.FromResult(Result<SeoContentDocument>.Success(doc));
        }

        public Task<Result<SeoContentDocument>> MigrateBlogSpokeChildIfAbsentAsync(
            Guid userId,
            Guid pillarDocumentId,
            MigrateBlogSpokeChildPayload payload,
            CancellationToken ct = default)
        {
            var existing = Documents.FirstOrDefault(d => d.ParentDocumentId == pillarDocumentId);
            if (existing is not null)
                return Task.FromResult(Result<SeoContentDocument>.Success(existing));

            return CreateAndPopulateAsync(userId, payload);
        }

        private async Task<Result<SeoContentDocument>> CreateAndPopulateAsync(
            Guid userId,
            MigrateBlogSpokeChildPayload payload)
        {
            var created = await CreateAsync(userId, payload.Child);
            if (!created.IsSuccess || created.Value is null)
                return created;

            var updated = await UpdateContentAsync(
                created.Value.Id,
                new UpdateContentRequest
                {
                    ContentHtml = payload.ContentHtml,
                    Title = payload.Child.Title,
                    TargetKeyword = payload.Child.TargetKeyword,
                    TargetLocation = payload.Child.TargetLocation,
                },
                payload.WordCount);
            if (!updated.IsSuccess || updated.Value is null)
                return updated;

            return await UpdateStatusAsync(created.Value.Id, payload.Status);
        }

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

    private sealed class NoOpAiProvider : IAIProvider
    {
        public string ProviderName => "noop";

        public Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<AIResponse>.Failure("AI not configured"));
    }
}
