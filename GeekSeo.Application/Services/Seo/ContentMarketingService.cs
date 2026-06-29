using System.Text.Json;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentMarketingService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    IAIProvider ai) : IContentMarketingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<ContentMarketingBundle>> GetBundleAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentMarketingBundle>.Failure(access.Error ?? "Access denied");

        return Result<ContentMarketingBundle>.Success(LoadBundle(access.Value));
    }

    public async Task<Result<ContentMarketingBundle>> SaveBundleAsync(
        Guid userId, Guid documentId, ContentMarketingBundle bundle, CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentMarketingBundle>.Failure(access.Error ?? "Access denied");

        if (string.IsNullOrWhiteSpace(bundle.PrimaryKeyword))
            bundle.PrimaryKeyword = access.Value.TargetKeyword;

        var saved = await documentRepo.UpdateMarketingBundleAsync(
            documentId, ContentMarketingBundleSerializer.Serialize(bundle), ct);
        if (!saved.IsSuccess || saved.Value is null)
            return Result<ContentMarketingBundle>.Failure(saved.Error ?? "Failed to save marketing bundle");

        return Result<ContentMarketingBundle>.Success(LoadBundle(saved.Value));
    }

    public async Task<Result<ContentMarketingBundle>> GenerateSummariesAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await RequirePillarAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentMarketingBundle>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        var bundle = LoadBundle(doc);

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentMarketingPromptBuilder.BuildSummariesSystemPrompt(),
            UserPrompt = ContentMarketingPromptBuilder.BuildSummariesUserPrompt(
                doc.Title, bundle.PrimaryKeyword, doc.ContentHtml),
            MaxTokens = 1024,
            Temperature = 0.5,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return Result<ContentMarketingBundle>.Failure(response.Error ?? "Summary generation failed");

        if (!TryParseSummaries(response.Value.Content, out var home, out var hub, out var meta))
            return Result<ContentMarketingBundle>.Failure("Could not parse summary JSON from AI response.");

        bundle.HomeSummary = home;
        bundle.HubSummary = hub;
        bundle.MetaDescription = meta;

        return await SaveBundleAsync(userId, documentId, bundle, ct);
    }

    public async Task<Result<ContentMarketingBundle>> GenerateBlogSpokeAsync(
        Guid userId, Guid documentId, GenerateBlogSpokeRequest request, CancellationToken ct = default)
    {
        var access = await RequirePillarAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentMarketingBundle>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        var bundle = LoadBundle(doc);
        var spokeType = string.IsNullOrWhiteSpace(request.SpokeType) ? "comparison" : request.SpokeType.Trim();

        var draft = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentMarketingPromptBuilder.BuildBlogSpokeSystemPrompt(),
            UserPrompt = ContentMarketingPromptBuilder.BuildBlogSpokeUserPrompt(
                doc.Title,
                bundle.PrimaryKeyword,
                spokeType,
                request.SpokeKeyword,
                doc.ContentHtml),
            MaxTokens = 8192,
            Temperature = 0.7,
        }, ct);

        if (!draft.IsSuccess || draft.Value is null)
            return Result<ContentMarketingBundle>.Failure(draft.Error ?? "Blog spoke generation failed");

        var spokeHtml = AiHtmlSanitizer.ToHtmlFragment(draft.Value.Content);

        var metaResponse = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentMarketingPromptBuilder.BuildBlogSpokeMetadataSystemPrompt(),
            UserPrompt = ContentMarketingPromptBuilder.BuildBlogSpokeMetadataUserPrompt(
                spokeType, bundle.PrimaryKeyword, spokeHtml),
            MaxTokens = 512,
            Temperature = 0.3,
        }, ct);

        if (!metaResponse.IsSuccess || metaResponse.Value is null)
            return Result<ContentMarketingBundle>.Failure(metaResponse.Error ?? "Blog spoke metadata failed");

        if (!TryParseBlogMetadata(metaResponse.Value.Content, out var title, out var slug, out var keyword, out var excerpt, out var metaDesc))
            return Result<ContentMarketingBundle>.Failure("Could not parse blog spoke metadata JSON.");

        bundle.BlogSpoke = new ContentMarketingBlogSpoke
        {
            Slug = slug,
            PrimaryKeyword = string.IsNullOrWhiteSpace(request.SpokeKeyword) ? keyword : request.SpokeKeyword.Trim(),
            SpokeType = spokeType,
            Title = title,
            ContentHtml = spokeHtml,
            Excerpt = excerpt,
            MetaDescription = metaDesc,
        };

        return await SaveBundleAsync(userId, documentId, bundle, ct);
    }

    public async Task<Result<ContentMarketingBundle>> GenerateSocialAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await RequirePillarAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<ContentMarketingBundle>.Failure(access.Error ?? "Access denied");

        var doc = access.Value;
        var bundle = LoadBundle(doc);

        if (string.IsNullOrWhiteSpace(bundle.UseCaseSlug))
            return Result<ContentMarketingBundle>.Failure("Set use case slug before generating social posts.");

        var response = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentMarketingPromptBuilder.BuildSocialSystemPrompt(),
            UserPrompt = ContentMarketingPromptBuilder.BuildSocialUserPrompt(bundle, doc.Title, doc.ContentHtml),
            MaxTokens = 2048,
            Temperature = 0.7,
        }, ct);

        if (!response.IsSuccess || response.Value is null)
            return Result<ContentMarketingBundle>.Failure(response.Error ?? "Social generation failed");

        if (!TryParseSocial(response.Value.Content, bundle, out var social))
            return Result<ContentMarketingBundle>.Failure("Could not parse social JSON from AI response.");

        bundle.Social = social;
        return await SaveBundleAsync(userId, documentId, bundle, ct);
    }

    public ContentMarketingValidationResult Validate(ContentMarketingBundle bundle) =>
        ContentMarketingValidator.Validate(bundle);

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
                "Write or generate the pillar article before creating marketing assets.");
        }

        return access;
    }

    private static ContentMarketingBundle LoadBundle(SeoContentDocument doc)
    {
        var bundle = ContentMarketingBundleSerializer.Parse(doc.MarketingBundleJson, doc.TargetKeyword);
        if (string.IsNullOrWhiteSpace(bundle.UseCaseSlug))
            bundle.UseCaseSlug = Slugify(doc.Title);
        if (string.IsNullOrWhiteSpace(bundle.PrimaryKeyword))
            bundle.PrimaryKeyword = doc.TargetKeyword;
        return bundle;
    }

    private static bool TryParseSummaries(
        string raw, out string home, out string hub, out string meta)
    {
        home = hub = meta = string.Empty;
        if (!TryParseJsonObject(raw, out var root))
            return false;

        home = root.GetProperty("homeSummary").GetString()?.Trim() ?? string.Empty;
        hub = root.GetProperty("hubSummary").GetString()?.Trim() ?? string.Empty;
        meta = root.GetProperty("metaDescription").GetString()?.Trim() ?? string.Empty;
        return home.Length > 0 && hub.Length > 0 && meta.Length > 0;
    }

    private static bool TryParseBlogMetadata(
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

    private static bool TryParseSocial(
        string raw,
        ContentMarketingBundle bundle,
        out ContentMarketingSocial social)
    {
        social = new ContentMarketingSocial();
        if (!TryParseJsonObject(raw, out var root))
            return false;

        if (!root.TryGetProperty("linkedin", out var li))
            return false;
        if (!root.TryGetProperty("facebook", out var fb))
            return false;

        social.LinkedIn = ParseSocialPost(li, "pillar", bundle.UseCaseSlug);
        var blogSlug = bundle.BlogSpoke?.Slug ?? bundle.UseCaseSlug;
        social.Facebook = ParseSocialPost(fb, "blog", blogSlug);
        return social.LinkedIn.Body.Length > 0 && social.Facebook.Body.Length > 0;
    }

    private static ContentMarketingSocialPost ParseSocialPost(
        JsonElement element, string defaultKind, string defaultSlug)
    {
        var kind = element.TryGetProperty("linkTargetKind", out var k)
            ? k.GetString()?.Trim() ?? defaultKind
            : defaultKind;
        var slug = element.TryGetProperty("linkTargetSlug", out var s)
            ? s.GetString()?.Trim() ?? defaultSlug
            : defaultSlug;
        var body = element.GetProperty("body").GetString()?.Trim() ?? string.Empty;
        return new ContentMarketingSocialPost
        {
            Body = body,
            LinkTargetKind = kind,
            LinkTargetSlug = slug,
        };
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

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var lower = value.ToLowerInvariant();
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return slug.Trim('-');
    }
}
