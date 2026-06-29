using System.Text.Json;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentBlogSpokeService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    IAIProvider ai) : IContentBlogSpokeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<ContentBlogSpoke>> GetAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentBlogSpoke>.Failure(access.Error ?? "Access denied");

        var spoke = Parse(access.Value.BlogSpokeJson);
        return spoke is null
            ? Result<ContentBlogSpoke>.Failure("No blog version yet.")
            : Result<ContentBlogSpoke>.Success(spoke);
    }

    public async Task<Result<ContentBlogSpoke>> SaveAsync(
        Guid userId, Guid documentId, ContentBlogSpoke spoke, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentBlogSpoke>.Failure(access.Error ?? "Access denied");

        var validation = Validate(access.Value.TargetKeyword, spoke);
        if (!validation.IsValid)
            return Result<ContentBlogSpoke>.Failure(string.Join(' ', validation.Errors));

        var saved = await documentRepo.UpdateBlogSpokeAsync(documentId, Serialize(spoke), ct);
        if (!saved.IsSuccess || saved.Value is null)
            return Result<ContentBlogSpoke>.Failure(saved.Error ?? "Failed to save blog version");

        return Result<ContentBlogSpoke>.Success(Parse(saved.Value.BlogSpokeJson) ?? spoke);
    }

    public async Task<Result<ContentBlogSpoke>> GenerateAsync(
        Guid userId, Guid documentId, GenerateBlogSpokeRequest request, CancellationToken ct = default)
    {
        var access = await RequirePillarAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentBlogSpoke>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        var spokeType = string.IsNullOrWhiteSpace(request.SpokeType) ? "comparison" : request.SpokeType.Trim();

        var draft = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentBlogSpokePromptBuilder.BuildSystemPrompt(),
            UserPrompt = ContentBlogSpokePromptBuilder.BuildUserPrompt(
                doc.Title,
                doc.TargetKeyword,
                spokeType,
                request.SpokeKeyword,
                doc.ContentHtml),
            MaxTokens = 8192,
            Temperature = 0.7,
        }, ct);

        if (!draft.IsSuccess || draft.Value is null)
            return Result<ContentBlogSpoke>.Failure(draft.Error ?? "Blog generation failed");

        var spokeHtml = AiHtmlSanitizer.ToHtmlFragment(draft.Value.Content);

        var metaResponse = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentBlogSpokePromptBuilder.BuildMetadataSystemPrompt(),
            UserPrompt = ContentBlogSpokePromptBuilder.BuildMetadataUserPrompt(
                spokeType, doc.TargetKeyword, spokeHtml),
            MaxTokens = 512,
            Temperature = 0.3,
        }, ct);

        if (!metaResponse.IsSuccess || metaResponse.Value is null)
            return Result<ContentBlogSpoke>.Failure(metaResponse.Error ?? "Blog metadata generation failed");

        if (!TryParseMetadata(metaResponse.Value.Content, out var title, out var slug, out var keyword, out var excerpt, out var metaDesc))
            return Result<ContentBlogSpoke>.Failure("Could not parse blog metadata JSON from AI response.");

        var spoke = new ContentBlogSpoke
        {
            Slug = slug,
            PrimaryKeyword = string.IsNullOrWhiteSpace(request.SpokeKeyword) ? keyword : request.SpokeKeyword.Trim(),
            SpokeType = spokeType,
            Title = title,
            ContentHtml = spokeHtml,
            Excerpt = excerpt,
            MetaDescription = metaDesc,
        };

        return await SaveAsync(userId, documentId, spoke, ct);
    }

    public async Task<Result<ContentBlogSpoke>> AddFaqsAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var spokeResult = await GetAsync(userId, documentId, ct);
        if (!spokeResult.IsSuccess || spokeResult.Value is null)
            return Result<ContentBlogSpoke>.Failure(spokeResult.Error ?? "No blog version yet.");

        var spoke = spokeResult.Value;
        string updatedHtml;

        if (!ArticleClosingFaqEnricher.HasCompleteClosingFaqSection(spoke.ContentHtml))
        {
            updatedHtml = await ArticleClosingFaqEnricher.EnsureClosingFaqDraftAsync(
                spoke.ContentHtml,
                spoke.PrimaryKeyword,
                [],
                ai,
                ct);
        }
        else
        {
            updatedHtml = await ArticleClosingFaqEnricher.AppendAdditionalClosingFaqsAsync(
                spoke.ContentHtml,
                spoke.PrimaryKeyword,
                additionalCount: 2,
                ai,
                ct);
        }

        if (string.Equals(updatedHtml, spoke.ContentHtml, StringComparison.Ordinal))
            return Result<ContentBlogSpoke>.Failure("Could not add FAQs to the blog version.");

        spoke.ContentHtml = updatedHtml;
        return await SaveAsync(userId, documentId, spoke, ct);
    }

    public ContentBlogSpokeValidationResult Validate(string pillarKeyword, ContentBlogSpoke spoke) =>
        ContentBlogSpokeValidator.Validate(pillarKeyword, spoke);

    private async Task<Result<SeoContentDocument>> RequirePillarAsync(
        Guid userId, Guid documentId, CancellationToken ct)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<SeoContentDocument>.Failure(access.Error ?? "Access denied");

        if (string.IsNullOrWhiteSpace(access.Value.ContentHtml) ||
            access.Value.ContentHtml.Length < 200)
        {
            return Result<SeoContentDocument>.Failure(
                "Write or generate the pillar article before creating a blog version.");
        }

        return access;
    }

    private static ContentBlogSpoke? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<ContentBlogSpoke>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Serialize(ContentBlogSpoke spoke) =>
        JsonSerializer.Serialize(spoke, JsonOptions);

    private static bool TryParseMetadata(
        string raw,
        out string title,
        out string slug,
        out string keyword,
        out string excerpt,
        out string metaDescription)
    {
        title = slug = keyword = excerpt = metaDescription = string.Empty;
        if (!TryParseJsonObject(raw, out var root))
            return false;

        title = root.GetProperty("title").GetString()?.Trim() ?? string.Empty;
        slug = root.GetProperty("slug").GetString()?.Trim() ?? string.Empty;
        keyword = root.GetProperty("primaryKeyword").GetString()?.Trim() ?? string.Empty;
        excerpt = root.TryGetProperty("excerpt", out var ex) ? ex.GetString()?.Trim() ?? string.Empty : string.Empty;
        metaDescription = root.TryGetProperty("metaDescription", out var md)
            ? md.GetString()?.Trim() ?? string.Empty
            : string.Empty;
        return title.Length > 0 && slug.Length > 0 && keyword.Length > 0;
    }

    private static bool TryParseJsonObject(string raw, out JsonElement root)
    {
        root = default;
        var trimmed = raw.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed[start..(end + 1)]);
            root = doc.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
