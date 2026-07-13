using System.Net.Http.Json;
using System.Text.Json;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Export;
using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentWriter.Application.Services.Publish;

public interface IGeekBlogPublishService
{
    Task<GeekBlogPublishResult> PublishAsync(
        Guid projectId,
        string? departmentOverride = null,
        CancellationToken cancellationToken = default);
}

public sealed record PublishedGeekPost(
    string PostType,
    string Slug,
    int PostId,
    bool Created,
    string PublicPath);

public sealed record GeekBlogPublishResult(
    string Department,
    IReadOnlyList<PublishedGeekPost> Posts);

public class GeekBlogPublishService : IGeekBlogPublishService
{
    private const int PillarBodyMinWords = 200;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IProjectRepository _projectRepository;
    private readonly IProjectPublicationRepository _publicationRepository;
    private readonly IContentFigureRepository _figureRepository;
    private readonly CompanyProfileOptions _companyProfile;
    private readonly GeekBlogPublishOptions _publishOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeekBlogPublishService> _logger;

    public GeekBlogPublishService(
        IProjectRepository projectRepository,
        IProjectPublicationRepository publicationRepository,
        IContentFigureRepository figureRepository,
        IOptions<CompanyProfileOptions> companyProfile,
        IOptions<GeekBlogPublishOptions> publishOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<GeekBlogPublishService> logger)
    {
        _projectRepository = projectRepository;
        _publicationRepository = publicationRepository;
        _figureRepository = figureRepository;
        _companyProfile = companyProfile.Value;
        _publishOptions = publishOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<GeekBlogPublishResult> PublishAsync(
        Guid projectId,
        string? departmentOverride = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var project = await _projectRepository.GetWithDetailsAsync(projectId, cancellationToken)
            ?? throw new ContentGenerationException($"Project {projectId} was not found.");

        var contentSet = GeneratedContentSetAssembler.Assemble(
            project,
            _companyProfile.ArticleBaseUrl,
            _companyProfile.BlogBaseUrl,
            departmentOverride);

        var department = contentSet.Department;
        if (!SiteDepartments.IsKnown(department))
        {
            throw new ContentGenerationException(
                $"Department '{department}' is not valid for geekatyourspot. " +
                $"Choose one of: {string.Join(", ", SiteDepartments.All)}.");
        }

        department = SiteDepartments.Normalize(department);
        var published = new List<PublishedGeekPost>();
        var existingPosts = await LoadExistingPostsAsync(cancellationToken);
        var requirePaa = project.KeywordSources.Any(k => k.Category == KeywordSourceCategory.PeopleAlsoAsk);

        if (contentSet.Article is not null
            && contentSet.Article.WordCount >= PillarBodyMinWords
            && !string.IsNullOrWhiteSpace(contentSet.ArticleSlug))
        {
            var articleRow = RequireRow(project, GeneratedContentType.TechnicalArticle);
            var apiSlug = GeekPublicUrlBuilder.ApiSlugForArticle(department, contentSet.ArticleSlug);
            var markdown = HtmlToMarkdownConverter.Convert(articleRow.BodyHtml);
            PillarMarkdownValidator.Validate(
                markdown,
                GeneratedContentPresentation.PublishTitle(articleRow),
                requirePaa);

            var post = await UpsertPostAsync(
                existingPosts,
                projectId,
                department,
                contentRole: "pillar",
                sourcePillarSlug: null,
                postType: "TechnicalArticle",
                apiSlug,
                GeneratedContentPresentation.PublishTitle(articleRow),
                markdown,
                articleRow.JsonLdSchema ?? "{}",
                articleRow,
                cancellationToken);

            published.Add(post with { PublicPath = GeekPublicUrlBuilder.ArticlePath(department, contentSet.ArticleSlug) });
            await StampFiguresAsync(projectId, FigureSourceType.Pillar, apiSlug, post.PostId, cancellationToken);
        }

        if (contentSet.Blog is not null
            && contentSet.Blog.WordCount > 0
            && !string.IsNullOrWhiteSpace(contentSet.BlogSlug))
        {
            var blogRow = RequireRow(project, GeneratedContentType.BlogPost);
            var apiSlug = GeekPublicUrlBuilder.ApiSlugForBlog(department, contentSet.BlogSlug);
            var post = await UpsertPostAsync(
                existingPosts,
                projectId,
                department,
                contentRole: "blog",
                sourcePillarSlug: contentSet.ArticleSlug,
                postType: "BlogPosting",
                apiSlug,
                GeneratedContentPresentation.PublishTitle(blogRow),
                HtmlToMarkdownConverter.Convert(blogRow.BodyHtml),
                blogRow.JsonLdSchema ?? "{}",
                blogRow,
                cancellationToken);

            published.Add(post with { PublicPath = GeekPublicUrlBuilder.BlogPath(department, contentSet.BlogSlug) });
            await StampFiguresAsync(projectId, FigureSourceType.Blog, apiSlug, post.PostId, cancellationToken);
        }

        var toolRows = project.GeneratedContents
            .Where(c => c.ContentType == GeneratedContentType.ToolPost)
            .OrderBy(c => c.SourceAppOrder ?? int.MaxValue)
            .ToList();

        foreach (var toolRow in toolRows)
        {
            if (toolRow.WordCount <= 0 || string.IsNullOrWhiteSpace(toolRow.Slug))
            {
                continue;
            }

            var apiSlug = GeekPublicUrlBuilder.ApiSlugForTool(department, toolRow.Slug);
            var post = await UpsertPostAsync(
                existingPosts,
                projectId,
                department,
                contentRole: "tool",
                sourcePillarSlug: contentSet.ArticleSlug,
                postType: "TechnicalArticle",
                apiSlug,
                GeneratedContentPresentation.PublishTitle(toolRow),
                HtmlToMarkdownConverter.Convert(toolRow.BodyHtml),
                toolRow.JsonLdSchema ?? "{}",
                toolRow,
                cancellationToken);

            published.Add(post with { PublicPath = GeekPublicUrlBuilder.ToolPath(department, toolRow.Slug) });
            var toolSource = FigureSourceType.ForTool(toolRow.Slug);
            await StampFiguresAsync(projectId, toolSource, apiSlug, post.PostId, cancellationToken);
        }

        if (published.Count == 0)
        {
            throw new ContentGenerationException(
                "Nothing to publish. Generate pillar body and/or blog content first.");
        }

        await _projectRepository.SaveChangesAsync(cancellationToken);

        foreach (var post in published)
        {
            await RevalidatePathAsync(post.PublicPath, cancellationToken);
        }

        return new GeekBlogPublishResult(department, published);
    }

    private async Task StampFiguresAsync(
        Guid projectId,
        string sourceType,
        string apiSlug,
        int postId,
        CancellationToken cancellationToken) =>
        await _figureRepository.StampAfterTextPublishAsync(
            projectId,
            sourceType,
            apiSlug,
            postId,
            cancellationToken);

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_publishOptions.ApiKey))
        {
            throw new ContentGenerationException(
                "GeekBlog:ApiKey is not configured. Set GEEK_BACKEND_API_KEY in the Content Writer API environment.");
        }
    }

    private static GeneratedContent RequireRow(Project project, GeneratedContentType type) =>
        project.GeneratedContents.FirstOrDefault(c => c.ContentType == type)
        ?? throw new ContentGenerationException($"Missing generated content of type {type}.");

    private async Task<PublishedGeekPost> UpsertPostAsync(
        IReadOnlyList<GeekBlogAdminPost> existingPosts,
        Guid projectId,
        string departmentSlug,
        string contentRole,
        string? sourcePillarSlug,
        string postType,
        string apiSlug,
        string title,
        string markdownBody,
        string schemaMetadataJson,
        GeneratedContent sourceRow,
        CancellationToken cancellationToken)
    {
        var strippedBody = GeekPublishPresentationHelper.StripMergedFigures(markdownBody);

        var payload = new
        {
            postType,
            status = "published",
            languageCode = "en",
            slug = apiSlug,
            title,
            body = strippedBody,
            schemaMetadataJson,
            tagSlugs = Array.Empty<string>(),
            publishedAt = DateTimeOffset.UtcNow,
            departmentSlug,
            presentation = GeneratedContentPresentation.BuildPresentationMap(sourceRow, contentRole),
            sourceProjectId = projectId,
            contentRole,
            sourcePillarSlug,
        };

        var postId = ResolveExistingPostId(existingPosts, postType, apiSlug);
        var hasExisting = postId is not null;
        using var request = new HttpRequestMessage(
            hasExisting ? HttpMethod.Put : HttpMethod.Post,
            hasExisting ? $"api/blog/{postId}" : "api/blog")
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };
        request.Headers.Add("X-API-Key", _publishOptions.ApiKey);

        var client = CreateGeekApiClient();
        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ContentGenerationException(
                $"GeekAPI {(hasExisting ? "update" : "create")} failed for {apiSlug}: {(int)response.StatusCode} {body}");
        }

        var created = hasExisting
            ? postId!.Value
            : (await response.Content.ReadFromJsonAsync<GeekBlogAdminPost>(JsonOptions, cancellationToken))?.PostId
              ?? throw new ContentGenerationException($"GeekAPI create succeeded but no post id for {apiSlug}");

        await _publicationRepository.UpsertAsync(
            new ProjectPublication
            {
                ProjectId = projectId,
                ContentType = MapContentType(contentRole),
                GeekPostId = created,
                GeekApiSlug = apiSlug,
                PublishedAtUtc = DateTime.UtcNow,
            },
            cancellationToken);

        _logger.LogInformation(
            "{Action} GeekAPI post {Slug} ({PostType}) as id {PostId}",
            hasExisting ? "Updated" : "Created",
            apiSlug,
            postType,
            created);

        return new PublishedGeekPost(postType, apiSlug, created, !hasExisting, string.Empty);
    }

    private static GeneratedContentType MapContentType(string contentRole) => contentRole switch
    {
        "pillar" => GeneratedContentType.TechnicalArticle,
        "blog" => GeneratedContentType.BlogPost,
        "tool" => GeneratedContentType.ToolPost,
        _ => GeneratedContentType.TechnicalArticle,
    };

    private async Task<IReadOnlyList<GeekBlogAdminPost>> LoadExistingPostsAsync(CancellationToken cancellationToken)
    {
        var client = CreateGeekApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/blog/all?lang=en");
        request.Headers.Add("X-API-Key", _publishOptions.ApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ContentGenerationException($"GeekAPI list failed: {(int)response.StatusCode} {body}");
        }

        return await response.Content.ReadFromJsonAsync<List<GeekBlogAdminPost>>(JsonOptions, cancellationToken)
            ?? [];
    }

    private static int? ResolveExistingPostId(
        IReadOnlyList<GeekBlogAdminPost> posts,
        string postType,
        string apiSlug)
    {
        var fullKey = SlugKey(apiSlug, "en");
        var exact = posts.FirstOrDefault(p => SlugKey(p.Slug, p.LanguageCode) == fullKey);
        if (exact is not null)
        {
            return exact.PostId;
        }

        var pageSlug = apiSlug.Split('/').Last();
        var suffix = "/" + pageSlug;
        var match = posts.FirstOrDefault(p =>
            p.PostType.Equals(postType, StringComparison.OrdinalIgnoreCase)
            && p.Slug.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        return match?.PostId;
    }

    private async Task RevalidatePathAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_publishOptions.RevalidateSecret))
        {
            _logger.LogWarning("GeekBlog:RevalidateSecret is not set; skipping revalidate for {Path}", path);
            return;
        }

        var baseUrl = _publishOptions.SiteBaseUrl.TrimEnd('/');
        var url =
            $"{baseUrl}/api/revalidate?secret={Uri.EscapeDataString(_publishOptions.RevalidateSecret)}&path={Uri.EscapeDataString(path)}";

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Revalidate failed for {Path}: {(Status)} {Body}",
                path,
                (int)response.StatusCode,
                body);
        }
    }

    private HttpClient CreateGeekApiClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(GeekBlogPublishService));
        client.BaseAddress = new Uri(_publishOptions.ApiUrl.TrimEnd('/') + "/");
        return client;
    }

    private static string SlugKey(string slug, string languageCode) =>
        $"{languageCode}:{slug}".ToLowerInvariant();

    private sealed record GeekBlogAdminPost(int PostId, string Slug, string LanguageCode, string PostType);
}
