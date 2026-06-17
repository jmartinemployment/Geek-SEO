using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class ContentWritingRenderTests
{
    [Fact]
    public void HtmlTextUtility_CountWords_IgnoresJsonLdScripts()
    {
        var html = """
            <h1>Zapier QuickBooks Integration</h1>
            <p>Map invoice sync and payment follow-up workflows.</p>
            <script type="application/ld+json">
            {"@context":"https://schema.org","@type":"FAQPage","mainEntity":[{"@type":"Question","name":"How much does it cost?"}]}
            </script>
            """;

        var wordCount = HtmlTextUtility.CountWords(html);
        var text = new HtmlRichTextProvider().ExtractPlainText(html);

        Assert.Equal(10, wordCount);
        Assert.DoesNotContain("FAQPage", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArticleRenderService_RenderAsync_AppendsSchemaScriptsWithoutMutatingBody()
    {
        var userId = Guid.NewGuid();
        var document = new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = userId,
            Title = "Zapier QuickBooks Integration",
            TargetKeyword = "zapier quickbooks integration",
            TargetLocation = "Palm Beach County, FL",
            ContentHtml = "<h1>Zapier QuickBooks Integration</h1><p>Body only.</p>",
            Status = "approved_for_publish",
        };

        var brief = new ContentBrief
        {
            Keyword = document.TargetKeyword,
            Location = document.TargetLocation,
            TargetWordCount = 1800,
            SchemaBlueprint = new SchemaBlueprint
            {
                PrimaryType = "TechArticle",
                AdditionalTypes = ["FAQPage"],
                SoftwareEntities = ["Zapier", "QuickBooks"],
            },
            PeopleAlsoAsk = ["How much does Zapier QuickBooks integration cost?"],
            AuthorOrganizationName = "Geek @ Your Spot",
            AuthorOrganizationUrl = "https://geekatyourspot.com",
        };

        var service = new ArticleRenderService(
            new FakeContentDocumentService(document),
            new FakeContentBriefService(brief),
            new FakeUrlResearchService());

        var result = await service.RenderAsync(userId, document.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(document.ContentHtml, result.Value!.BodyHtml);
        Assert.Contains("\"@type\":\"TechArticle\"", result.Value.RenderedHtml);
        Assert.Contains("\"@type\":\"SoftwareApplication\"", result.Value.RenderedHtml);
        Assert.Contains("FAQPage", string.Join(",", result.Value.SchemaTypes));
    }

    [Fact]
    public async Task WordPressPublishService_PublishDocumentAsync_RequiresApprovalBeforePublish()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var document = new SeoContentDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Title = "Draft awaiting review",
            ContentHtml = "<h1>Draft</h1><p>Pending approval.</p>",
            TargetKeyword = "draft keyword",
            TargetLocation = "United States",
            Status = "awaiting_review",
        };

        var service = new WordPressPublishService(
            new FakeProjectRepository(projectId, userId),
            new FakeContentDocumentService(document),
            new FakeArticleRenderService(),
            new FakeWordPressConnectionRepository(),
            new FakeWordPressProvider(),
            new FakeWordPressPublishRepository());

        var result = await service.PublishDocumentAsync(userId, document.Id, new WordPressPublishOptions());

        Assert.False(result.IsSuccess);
        Assert.Contains("approved for publish", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeContentDocumentService(SeoContentDocument document) : IContentDocumentService
    {
        public Task<Result<SeoContentDocument>> EnsureAccessAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoContentDocument>.Success(document));

        public Task<Result<IReadOnlyList<SeoContentDocument>>> ListByProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> GetAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoContentDocument>.Success(document));

        public Task<Result<SeoContentDocument>> CreateAsync(Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateContentAsync(Guid userId, Guid documentId, UpdateContentRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoContentDocument>> UpdateStatusAsync(Guid userId, Guid documentId, string status, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoContentDocument>.Success(new SeoContentDocument
            {
                Id = document.Id,
                ProjectId = document.ProjectId,
                UserId = document.UserId,
                Title = document.Title,
                ContentHtml = document.ContentHtml,
                TargetKeyword = document.TargetKeyword,
                TargetLocation = document.TargetLocation,
                Status = status,
            }));

        public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
            Guid userId, Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeContentBriefService(ContentBrief brief) : IContentBriefService
    {
        public Task<Result<ContentBrief>> GenerateBriefAsync(Guid userId, GenerateBriefRequest request, CancellationToken ct = default) =>
            Task.FromResult(Result<ContentBrief>.Success(brief));
    }

    private sealed class FakeUrlResearchService : IUrlResearchService
    {
        public Task<Result<SeoUrlResearch>> CreateQueuedAsync(Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> GetFullAsync(Guid userId, Guid urlResearchId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> PersistFullAsync(Guid userId, Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoUrlResearch>> UpdateStatusAsync(Guid userId, Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeProjectRepository(Guid projectId, Guid userId) : IProjectRepository
    {
        public Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid requestedUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoProject>> GetByIdAsync(Guid requestedProjectId, CancellationToken ct = default) =>
            Task.FromResult(Result<SeoProject>.Success(new SeoProject
            {
                Id = projectId,
                UserId = userId,
                Name = "Geek @ Your Spot",
                Url = "https://geekatyourspot.com",
            }));

        public Task<Result<SeoProject>> GetByIdAsync(Guid requestedProjectId, Guid requestedUserId, CancellationToken ct = default) =>
            GetByIdAsync(requestedProjectId, ct);

        public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeArticleRenderService : IArticleRenderService
    {
        public Task<Result<RenderedArticleResult>> RenderAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
            Task.FromResult(Result<RenderedArticleResult>.Success(new RenderedArticleResult
            {
                BodyHtml = "<h1>Draft</h1>",
                RenderedHtml = "<h1>Draft</h1><script type=\"application/ld+json\">{\"@type\":\"TechArticle\"}</script>",
                SchemaScripts = ["<script type=\"application/ld+json\">{\"@type\":\"TechArticle\"}</script>"],
                SchemaTypes = ["TechArticle"],
            }));
    }

    private sealed class FakeWordPressConnectionRepository : IWordPressConnectionRepository
    {
        public Task<Result<SeoWordPressConnection?>> GetByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<SeoWordPressConnection>> UpsertAsync(SeoWordPressConnection connection, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result> DeleteByProjectAsync(Guid projectId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeWordPressProvider : IWordPressProvider
    {
        public string ProviderName => "fake";

        public Task<Result<WordPressConnectionTestResult>> TestConnectionAsync(WordPressCredentials credentials, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Result<WordPressPublishProviderResult>> PublishPostAsync(WordPressCredentials credentials, WordPressPostPayload post, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeWordPressPublishRepository : IWordPressPublishRepository
    {
        public Task<Result> RecordPublishAsync(Guid projectId, Guid documentId, string targetKeyword, int wordCount, string title, string publishedUrl, int wordPressPostId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
