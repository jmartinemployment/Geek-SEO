using System.Net.Http.Json;
using System.Text.Json;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Publish;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentWriter.Application.Services.Figures;

public interface IFigureMergeService
{
    Task<FigureMergeResult> MergeSourceAsync(
        Guid projectId,
        string sourceType,
        CancellationToken cancellationToken = default);
}

public sealed record FigureMergeResult(
    string SourceType,
    string GeekApiSlug,
    int GeekPostId,
    int FiguresMerged,
    string PublicPath);

public sealed class FigureMergeService : IFigureMergeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IContentFigureRepository _figures;
    private readonly GeekBlogPublishOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FigureMergeService> _logger;

    public FigureMergeService(
        IContentFigureRepository figures,
        IOptions<GeekBlogPublishOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<FigureMergeService> logger)
    {
        _figures = figures;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FigureMergeResult> MergeSourceAsync(
        Guid projectId,
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        ValidateSourceType(sourceType);

        var rows = await _figures.ListTrackedByProjectAsync(projectId, cancellationToken);
        var candidates = rows
            .Where(f => f.SourceType == sourceType)
            .Where(f => f.Status is FigureStatus.Ready or FigureStatus.Published)
            .Where(f => !string.IsNullOrWhiteSpace(f.ImageUrl))
            .OrderBy(f => f.SectionOrder)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new ContentGenerationException(
                $"No Ready or Published figures with images for source '{sourceType}'.");
        }

        var geekApiSlug = candidates[0].GeekApiSlug;
        var geekPostId = candidates[0].GeekPostId;
        if (string.IsNullOrWhiteSpace(geekApiSlug) || geekPostId is null or <= 0)
        {
            throw new ContentGenerationException(
                "GeekApiSlug and GeekPostId are required. Publish text before merging figures.");
        }

        if (candidates.Any(f => f.GeekApiSlug != geekApiSlug || f.GeekPostId != geekPostId))
        {
            throw new ContentGenerationException(
                "All merge candidates must share the same GeekApiSlug and GeekPostId.");
        }

        var post = await GetPostByIdAsync(geekPostId.Value, cancellationToken);
        var blocks = candidates.Select(ToMergeBlock).ToList();
        var cleanBody = FigureMergeMarkdownComposer.StripMergedFigures(post.Body);
        var hero = blocks.OrderBy(b => b.SectionOrder).First();

        await PutPostAsync(
            geekPostId.Value,
            geekApiSlug,
            post.PostType,
            post.Title,
            cleanBody,
            post.SchemaMetadataJson,
            post.PublishedAt,
            post.BlogExcerpt,
            post.TechnicalArticleExcerpt,
            post.ToolExcerpt,
            post.AdvertisingExcerpt,
            hero.ImageUrl,
            cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var figure in candidates)
        {
            figure.Status = FigureStatus.Published;
            figure.NeedsFigureMerge = false;
            figure.UpdatedAtUtc = now;
            await _figures.UpdateAsync(figure, cancellationToken);
        }

        await _figures.SaveChangesAsync(cancellationToken);

        var publicPath = ResolvePublicPath(geekApiSlug);
        await RevalidatePathAsync(publicPath, cancellationToken);

        _logger.LogInformation(
            "Published {Count} figures to GeekAPI post {PostId} ({Slug}); hero_image_url set, body figures stripped",
            candidates.Count,
            geekPostId.Value,
            geekApiSlug);

        return new FigureMergeResult(
            sourceType,
            geekApiSlug,
            geekPostId.Value,
            candidates.Count,
            publicPath);
    }

    public static void ValidateSourceType(string sourceType)
    {
        if (!FigureSourceType.IsKnown(sourceType))
        {
            throw new ArgumentException(
                $"Source must be '{FigureSourceType.Pillar}', '{FigureSourceType.Blog}', or '{FigureSourceType.ToolPrefix}{{slug}}'.",
                nameof(sourceType));
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ContentGenerationException(
                "GeekBlog:ApiKey is not configured. Set GEEK_BACKEND_API_KEY in the Content Writer API environment.");
        }
    }

    private static FigureMergeBlock ToMergeBlock(ContentFigure figure) =>
        new(
            figure.SourceType,
            figure.HeadingSlug,
            figure.Heading,
            figure.SectionOrder,
            figure.ImageUrl!,
            string.IsNullOrWhiteSpace(figure.ImageAlt)
                ? FigureHeadingSlugResolver.DefaultImageAlt(figure.Heading)
                : figure.ImageAlt,
            figure.ImageWidth,
            figure.ImageHeight,
            figure.Status);

    private async Task<GeekBlogAdminPost> GetPostByIdAsync(int postId, CancellationToken cancellationToken)
    {
        var client = CreateGeekApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/blog/by-id/{postId}?lang=en");
        request.Headers.Add("X-API-Key", _options.ApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ContentGenerationException(
                $"GeekAPI get post {postId} failed: {(int)response.StatusCode} {body}");
        }

        return await response.Content.ReadFromJsonAsync<GeekBlogAdminPost>(JsonOptions, cancellationToken)
               ?? throw new ContentGenerationException($"GeekAPI returned empty body for post {postId}.");
    }

    private async Task PutPostAsync(
        int postId,
        string apiSlug,
        string postType,
        string title,
        string body,
        string schemaMetadataJson,
        DateTimeOffset? publishedAt,
        string? blogExcerpt,
        string? technicalArticleExcerpt,
        string? toolExcerpt,
        string? advertisingExcerpt,
        string? heroImageUrl,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            postType,
            status = "published",
            languageCode = "en",
            slug = apiSlug,
            title,
            body,
            schemaMetadataJson,
            tagSlugs = Array.Empty<string>(),
            publishedAt = publishedAt?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
            blogExcerpt,
            technicalArticleExcerpt,
            toolExcerpt,
            advertisingExcerpt,
            heroImageUrl,
        };

        var client = CreateGeekApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/blog/{postId}")
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };
        request.Headers.Add("X-API-Key", _options.ApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ContentGenerationException(
                $"GeekAPI update post {postId} failed: {(int)response.StatusCode} {error}");
        }
    }

    private async Task RevalidatePathAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RevalidateSecret))
        {
            _logger.LogWarning("GeekBlog:RevalidateSecret is not set; skipping revalidate for {Path}", path);
            return;
        }

        var baseUrl = _options.SiteBaseUrl.TrimEnd('/');
        var url =
            $"{baseUrl}/api/revalidate?secret={Uri.EscapeDataString(_options.RevalidateSecret)}&path={Uri.EscapeDataString(path)}";

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ContentGenerationException(
                $"Revalidate failed for {path}: {(int)response.StatusCode} {body}");
        }
    }

    private HttpClient CreateGeekApiClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(FigureMergeService));
        client.BaseAddress = new Uri(_options.ApiUrl.TrimEnd('/') + "/");
        return client;
    }

    private static string ResolvePublicPath(string geekApiSlug)
    {
        if (geekApiSlug.StartsWith("use-cases/", StringComparison.OrdinalIgnoreCase))
        {
            return "/" + geekApiSlug;
        }

        if (geekApiSlug.StartsWith("blog/", StringComparison.OrdinalIgnoreCase))
        {
            return "/" + geekApiSlug;
        }

        if (geekApiSlug.StartsWith("tools/", StringComparison.OrdinalIgnoreCase))
        {
            return "/" + geekApiSlug;
        }

        throw new ContentGenerationException($"Unsupported GeekApiSlug for revalidate: {geekApiSlug}");
    }

    private sealed record GeekBlogAdminPost(
        int PostId,
        string PostType,
        string Title,
        string Body,
        string SchemaMetadataJson,
        DateTimeOffset? PublishedAt,
        string? BlogExcerpt,
        string? TechnicalArticleExcerpt,
        string? ToolExcerpt,
        string? AdvertisingExcerpt,
        string? HeroImageUrl);
}
