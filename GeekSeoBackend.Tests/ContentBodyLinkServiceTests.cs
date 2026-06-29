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
        };
        var documents = new FakeDocumentService(doc);
        var service = new ContentBodyLinkService(documents);

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
        Assert.Contains("related-guide-box", doc.ContentHtml, StringComparison.Ordinal);
    }

    private sealed class FakeDocumentService(SeoContentDocument document) : IContentDocumentService
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
            Guid userId, Guid documentId, Guid analysisRunId, string targetKeyword, string serpKeyword, Guid? siteProfileId = null, CancellationToken ct = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotImplementedException();
    }
}
