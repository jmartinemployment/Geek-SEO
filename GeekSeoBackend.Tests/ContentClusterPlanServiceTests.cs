using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentClusterPlanServiceTests
{
    [Fact]
    public async Task GetAsync_returns_empty_plan_when_json_missing()
    {
        var doc = PillarDocument();
        var service = new ContentClusterPlanService(
            new FakeDocumentService(doc),
            new FakeDocumentRepository(doc),
            AnalysisRunTestData.CreateContextLoader());

        var result = await service.GetAsync(doc.UserId, doc.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.FaqItems);
        Assert.Empty(result.Value.BodyLinks);
    }

    [Fact]
    public async Task SaveAsync_persists_and_returns_plan()
    {
        var doc = PillarDocument();
        var repo = new FakeDocumentRepository(doc);
        var service = new ContentClusterPlanService(new FakeDocumentService(doc), repo, AnalysisRunTestData.CreateContextLoader());
        var plan = new ContentLinkPlan
        {
            FaqItems =
            [
                new ContentLinkFaqItem
                {
                    Question = "Are there free AI market research tools?",
                    Source = "pasf",
                },
            ],
        };

        var result = await service.SaveAsync(doc.UserId, doc.Id, plan);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.FaqItems);
        Assert.NotNull(repo.LastLinkPlanJson);
        Assert.Contains("free AI market research tools", repo.LastLinkPlanJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_rejects_spoke_documents()
    {
        var doc = PillarDocument();
        doc.DocumentKind = ContentDocumentKinds.Spoke;
        var service = new ContentClusterPlanService(
            new FakeDocumentService(doc),
            new FakeDocumentRepository(doc),
            AnalysisRunTestData.CreateContextLoader());

        var result = await service.SaveAsync(doc.UserId, doc.Id, new ContentLinkPlan());

        Assert.False(result.IsSuccess);
        Assert.Contains("pillar", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildAsync_runs_planner_and_persists_faq_plan()
    {
        var export = AnalysisRunTestData.MarketResearchPasfExport();
        var doc = AnalysisRunTestData.FrozenResearchDocument(export, "ai market research tools");
        var repo = new FakeDocumentRepository(doc);
        var loader = AnalysisRunTestData.CreateContextLoader(
            export,
            AnalysisRunTestData.MinimalSiteBundle() with
            {
                BusinessSummary = "Paid AI market intelligence platform for enterprise teams.",
            });

        var service = new ContentClusterPlanService(new FakeDocumentService(doc), repo, loader);

        var result = await service.BuildAsync(doc.UserId, doc.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.SpokeCandidates.Count);
        Assert.Contains(result.Value.FilteredOut, f => f.RejectReason.Contains("course", StringComparison.Ordinal));
        Assert.Equal(5, result.Value.FaqItems.Count);
        Assert.NotNull(repo.LastLinkPlanJson);
        Assert.Contains("faqItems", repo.LastLinkPlanJson!, StringComparison.Ordinal);
    }

    private static SeoContentDocument PillarDocument() => new()
    {
        Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        DocumentKind = ContentDocumentKinds.Standalone,
        TargetKeyword = "AI market research",
    };

    private sealed class FakeDocumentService(SeoContentDocument document) : IContentDocumentService
    {
        public Task<Result<SeoContentDocument>> EnsureAccessAsync(
            Guid userId, Guid documentId, CancellationToken ct = default) =>
            userId == document.UserId && documentId == document.Id
                ? Task.FromResult(Result<SeoContentDocument>.Success(document))
                : Task.FromResult(Result<SeoContentDocument>.Failure("Access denied"));

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

    private sealed class FakeDocumentRepository(SeoContentDocument document) : IContentDocumentRepository
    {
        public string? LastLinkPlanJson { get; private set; }

        public Task<Result<SeoContentDocument>> GetByIdAsync(Guid documentId, CancellationToken ct = default) =>
            documentId == document.Id
                ? Task.FromResult(Result<SeoContentDocument>.Success(document))
                : Task.FromResult(Result<SeoContentDocument>.NotFound("not found"));

        public Task<Result<IReadOnlyList<SeoContentDocument>>> GetByProjectAsync(
            Guid projectId, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> CreateAsync(
            Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result<SeoContentDocument>> UpdateContentAsync(
            Guid documentId, UpdateContentRequest request, int wordCount, CancellationToken ct = default) =>
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
            Guid documentId, string linkPlanJson, CancellationToken ct = default)
        {
            document.LinkPlanJson = linkPlanJson;
            LastLinkPlanJson = linkPlanJson;
            return Task.FromResult(Result<SeoContentDocument>.Success(document));
        }

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
