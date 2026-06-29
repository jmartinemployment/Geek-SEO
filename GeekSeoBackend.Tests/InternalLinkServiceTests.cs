using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class InternalLinkServiceTests
{
    private static readonly Guid ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task SuggestAsync_pillar_prefers_child_spokes_with_publish_paths()
    {
        var pillarId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var spokeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var pillar = Doc(pillarId, "AI market research tools", ContentDocumentKinds.Pillar);
        var spoke = Doc(
            spokeId,
            "best ai for market analysis",
            ContentDocumentKinds.Spoke,
            pillarId,
            d =>
            {
                d.PublishSlug = "best-ai-for-market-analysis";
                d.SpokeSourcePhrase = "best AI for market analysis";
                d.Title = "Best AI for market analysis";
            });

        var service = BuildService(pillar, spoke);

        var result = await service.SuggestAsync(
            UserId,
            new InternalLinkSuggestRequest { ProjectId = ProjectId, DocumentId = pillarId });

        Assert.True(result.IsSuccess);
        var suggestion = Assert.Single(result.Value!);
        Assert.Equal(spokeId, suggestion.TargetDocumentId);
        Assert.Equal(InternalLinkTypes.Spoke, suggestion.LinkType);
        Assert.Equal("/blog/best-ai-for-market-analysis", suggestion.TargetUrl);
        Assert.Equal("/blog/best-ai-for-market-analysis", suggestion.PublishPath);
    }

    [Fact]
    public async Task SuggestAsync_spoke_includes_parent_pillar_and_sibling_spokes()
    {
        var pillarId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var spokeA = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var spokeB = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var pillar = Doc(pillarId, "AI market research tools", ContentDocumentKinds.Pillar, configure: d =>
        {
            d.PublishSlug = "ai-market-research-tools";
        });
        var current = Doc(spokeA, "best ai for market analysis", ContentDocumentKinds.Spoke, pillarId, d =>
        {
            d.PublishSlug = "best-ai-for-market-analysis";
        });
        var sibling = Doc(spokeB, "ai market research companies", ContentDocumentKinds.Spoke, pillarId, d =>
        {
            d.PublishSlug = "ai-market-research-companies";
        });

        var service = BuildService(pillar, current, sibling);

        var result = await service.SuggestAsync(
            UserId,
            new InternalLinkSuggestRequest { ProjectId = ProjectId, DocumentId = spokeA, MaxSuggestions = 5 });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, s => s.LinkType == InternalLinkTypes.Pillar);
        Assert.Contains(result.Value, s => s.LinkType == InternalLinkTypes.Spoke && s.TargetDocumentId == spokeB);
    }

    [Fact]
    public async Task SuggestAsync_boosts_plan_priority_targets()
    {
        var pillarId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var spokeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var unrelatedId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var plan = new ContentLinkPlan
        {
            BodyLinks =
            [
                new ContentLinkBodySlot
                {
                    TargetDocumentId = spokeId,
                    TargetPath = "/blog/best-ai-for-market-analysis",
                    Priority = 5,
                },
            ],
        };
        var pillar = Doc(pillarId, "AI market research tools", ContentDocumentKinds.Pillar, configure: d =>
        {
            d.LinkPlanJson = ContentLinkPlanJson.Serialize(plan);
        });
        var spoke = Doc(spokeId, "best ai for market analysis", ContentDocumentKinds.Spoke, pillarId, d =>
        {
            d.PublishSlug = "best-ai-for-market-analysis";
        });
        var unrelated = Doc(unrelatedId, "ai market research report", ContentDocumentKinds.Standalone);

        var service = BuildService(pillar, spoke, unrelated);

        var result = await service.SuggestAsync(
            UserId,
            new InternalLinkSuggestRequest { ProjectId = ProjectId, DocumentId = pillarId, MaxSuggestions = 5 });

        Assert.True(result.IsSuccess);
        Assert.Equal(spokeId, result.Value!.First().TargetDocumentId);
    }

    private static InternalLinkService BuildService(params SeoContentDocument[] docs)
    {
        var project = new SeoProject
        {
            Id = ProjectId,
            UserId = UserId,
            Name = "Test",
            Url = "https://example.com",
        };
        return new InternalLinkService(
            new FakeProjectRepository(project),
            new FakeDocumentRepository(docs));
    }

    private static SeoContentDocument Doc(
        Guid id,
        string keyword,
        string kind,
        Guid? parentId = null,
        Action<SeoContentDocument>? configure = null)
    {
        var doc = new SeoContentDocument
        {
            Id = id,
            ProjectId = ProjectId,
            UserId = UserId,
            Title = keyword,
            TargetKeyword = keyword,
            DocumentKind = kind,
            ParentDocumentId = parentId,
            ContentHtml = "<p>Body</p>",
            Status = "writing",
        };
        configure?.Invoke(doc);
        return doc;
    }

    private sealed class FakeProjectRepository(SeoProject project) : IProjectRepository
    {
        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, CancellationToken ct = default) =>
            projectId == project.Id
                ? Task.FromResult(Result<SeoProject>.Success(project))
                : Task.FromResult(Result<SeoProject>.Failure("not found"));

        public Task<Result<SeoProject>> GetByIdAsync(Guid projectId, Guid userId, CancellationToken ct = default) =>
            projectId == project.Id && userId == project.UserId
                ? Task.FromResult(Result<SeoProject>.Success(project))
                : Task.FromResult(Result<SeoProject>.Failure("not found"));

        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeDocumentRepository(IReadOnlyList<SeoContentDocument> docs) : IContentDocumentRepository
    {
        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            docs.FirstOrDefault(d => d.Id == documentId) is { } doc
                ? Task.FromResult(Result<SeoContentDocument>.Success(doc))
                : Task.FromResult(Result<SeoContentDocument>.Failure("not found"));

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            Task.FromResult(Result<IReadOnlyList<SeoContentDocument>>.Success(
                docs.Where(d => d.ProjectId == projectId).ToList()));

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
}
