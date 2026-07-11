using System.Net.Http.Json;
using System.Text.Json;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Export;
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
    private readonly IContentFigureRepository _figureRepository;
    private readonly CompanyProfileOptions _companyProfile;
    private readonly GeekBlogPublishOptions _publishOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeekBlogPublishService> _logger;

    public GeekBlogPublishService(
        IProjectRepository projectRepository,
        IContentFigureRepository figureRepository,
        IOptions<CompanyProfileOptions> companyProfile,
        IOptions<GeekBlogPublishOptions> publishOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<GeekBlogPublishService> logger)
    {
        _projectRepository = projectRepository;
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
        var published = new List<PublishedGeekPost>();
        var existingBySlug = await LoadExistingPostsBySlugAsync(cancellationToken);

        if (contentSet.Article is not null
            && contentSet.Article.WordCount >= PillarBodyMinWords
            && !string.IsNullOrWhiteSpace(contentSet.ArticleSlug))
        {
            var apiSlug = GeekPublicUrlBuilder.ApiSlugForArticle(department, contentSet.ArticleSlug);
            var articleRow = RequireRow(project, GeneratedContentType.TechnicalArticle);
            var post = await UpsertPostAsync(
                existingBySlug,
                postType: "TechnicalArticle",
                apiSlug,
                articleRow.Title,
                HtmlToMarkdownConverter.Convert(articleRow.BodyHtml),
                articleRow.JsonLdSchema ?? "{}",
                cancellationToken);
            published.Add(post with { PublicPath = GeekPublicUrlBuilder.ArticlePath(department, contentSet.ArticleSlug) });
            await _figureRepository.StampAfterTextPublishAsync(
                projectId,
                FigureSourceType.Pillar,
                apiSlug,
                post.PostId,
                cancellationToken);
        }

        if (contentSet.Blog is not null
            && contentSet.Blog.WordCount > 0
            && !string.IsNullOrWhiteSpace(contentSet.BlogSlug))
        {
            var apiSlug = GeekPublicUrlBuilder.ApiSlugForBlog(department, contentSet.BlogSlug);
            var blogRow = RequireRow(project, GeneratedContentType.BlogPost);
            var post = await UpsertPostAsync(
                existingBySlug,
                postType: "BlogPosting",
                apiSlug,
                blogRow.Title,
                HtmlToMarkdownConverter.Convert(blogRow.BodyHtml),
                blogRow.JsonLdSchema ?? "{}",
                cancellationToken);
            published.Add(post with { PublicPath = GeekPublicUrlBuilder.BlogPath(department, contentSet.BlogSlug) });
            await _figureRepository.StampAfterTextPublishAsync(
                projectId,
                FigureSourceType.Blog,
                apiSlug,
                post.PostId,
                cancellationToken);
        }

        if (published.Count == 0)
        {
            throw new ContentGenerationException(
                "Nothing to publish. Generate pillar body and/or blog content first.");
        }

        foreach (var post in published)
        {
            await RevalidatePathAsync(post.PublicPath, cancellationToken);
        }

        return new GeekBlogPublishResult(department, published);
    }

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
        Dictionary<string, int> existingBySlug,
        string postType,
        string apiSlug,
        string title,
        string markdownBody,
        string schemaMetadataJson,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            postType,
            status = "published",
            languageCode = "en",
            slug = apiSlug,
            title,
            body = markdownBody,
            schemaMetadataJson,
            tagSlugs = Array.Empty<string>(),
            publishedAt = DateTimeOffset.UtcNow,
        };

        var hasExisting = existingBySlug.TryGetValue(SlugKey(apiSlug, "en"), out var postId);
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
            ? postId
            : (await response.Content.ReadFromJsonAsync<GeekBlogAdminPost>(JsonOptions, cancellationToken))?.PostId
              ?? throw new ContentGenerationException($"GeekAPI create succeeded but no post id for {apiSlug}");

        _logger.LogInformation(
            "{Action} GeekAPI post {Slug} ({PostType}) as id {PostId}",
            hasExisting ? "Updated" : "Created",
            apiSlug,
            postType,
            created);

        return new PublishedGeekPost(postType, apiSlug, created, !hasExisting, string.Empty);
    }

    private async Task<Dictionary<string, int>> LoadExistingPostsBySlugAsync(CancellationToken cancellationToken)
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

        var posts = await response.Content.ReadFromJsonAsync<List<GeekBlogAdminPost>>(JsonOptions, cancellationToken)
            ?? [];

        return posts.ToDictionary(p => SlugKey(p.Slug, p.LanguageCode), p => p.PostId);
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
            return;
        }

        _logger.LogInformation("Revalidated {Path}", path);
    }

    private HttpClient CreateGeekApiClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(GeekBlogPublishService));
        client.BaseAddress = new Uri(_publishOptions.ApiUrl.TrimEnd('/') + "/");
        return client;
    }

    private static string SlugKey(string slug, string languageCode) =>
        $"{languageCode}:{slug}".ToLowerInvariant();

    private sealed record GeekBlogAdminPost(int PostId, string Slug, string LanguageCode);
}
