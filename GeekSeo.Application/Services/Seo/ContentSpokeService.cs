using System.Net;
using System.Text.Json;
using GeekSeo.Application.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentSpokeService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo,
    IContentBlogSpokeMigrator migrator,
    IAIProvider ai) : IContentSpokeService
{
    public async Task<Result<IReadOnlyList<ContentSpokeSummary>>> ListAsync(
        Guid userId,
        Guid pillarDocumentId,
        CancellationToken ct = default)
    {
        var pillar = await RequirePillarAsync(userId, pillarDocumentId, ct);
        if (!pillar.IsSuccess || pillar.Value is null)
            return Result<IReadOnlyList<ContentSpokeSummary>>.Failure(pillar.Error ?? "Access denied");

        await migrator.EnsureMigratedChildAsync(userId, pillarDocumentId, ct);

        var projectDocs = await documentRepo.GetByProjectAsync(pillar.Value.ProjectId, ct);
        if (!projectDocs.IsSuccess || projectDocs.Value is null)
            return Result<IReadOnlyList<ContentSpokeSummary>>.Failure(projectDocs.Error ?? "Failed to load documents");

        var spokes = projectDocs.Value
            .Where(d => d.ParentDocumentId == pillarDocumentId)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(ToSummary)
            .ToList();

        return Result<IReadOnlyList<ContentSpokeSummary>>.Success(spokes);
    }

    public async Task<Result<ContentSpokeSummary>> CreateAsync(
        Guid userId,
        Guid pillarDocumentId,
        CreateContentSpokeRequest request,
        CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Phrase))
            return Result<ContentSpokeSummary>.Failure("Spoke phrase is required.");

        var pillarResult = await RequirePillarAsync(userId, pillarDocumentId, ct);
        if (!pillarResult.IsSuccess || pillarResult.Value is null)
            return Result<ContentSpokeSummary>.Failure(pillarResult.Error ?? "Access denied");

        var pillar = pillarResult.Value;
        var phrase = request.Phrase.Trim();
        var sourceType = string.IsNullOrWhiteSpace(request.SourceType)
            ? SpokeSourceTypes.Manual
            : request.SourceType.Trim().ToLowerInvariant();

        if (!SpokeSourceTypes.IsKnown(sourceType))
            return Result<ContentSpokeSummary>.Failure($"Unknown spoke source type \"{sourceType}\".");

        var projectDocs = await documentRepo.GetByProjectAsync(pillar.ProjectId, ct);
        if (!projectDocs.IsSuccess || projectDocs.Value is null)
            return Result<ContentSpokeSummary>.Failure(projectDocs.Error ?? "Failed to load project documents");

        if (projectDocs.Value.Any(d =>
                d.ParentDocumentId == pillarDocumentId &&
                string.Equals(d.SpokeSourcePhrase, phrase, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<ContentSpokeSummary>.Failure(
                $"A spoke for \"{phrase}\" already exists on this pillar.");
        }

        var existingSlugs = projectDocs.Value
            .Select(d => d.PublishSlug)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToList();

        var baseSlug = !string.IsNullOrWhiteSpace(request.PublishSlug)
            ? request.PublishSlug.Trim().ToLowerInvariant()
            : ContentPublishSlug.NormalizeFromPhrase(phrase);

        if (string.IsNullOrWhiteSpace(baseSlug))
            return Result<ContentSpokeSummary>.Failure("Could not derive a publish slug for this spoke.");

        var publishSlug = ContentPublishSlug.AllocateUnique(baseSlug, existingSlugs);
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? ContentClusterLinkPlanner.RewriteQuestion(phrase, pillar.TargetKeyword)
            : request.Title.Trim();
        var targetKeyword = string.IsNullOrWhiteSpace(request.TargetKeyword) ? phrase : request.TargetKeyword.Trim();

        var createResult = await documentRepo.CreateAsync(
            userId,
            new CreateContentDocumentRequest
            {
                ProjectId = pillar.ProjectId,
                ParentDocumentId = pillar.Id,
                DocumentKind = ContentDocumentKinds.Spoke,
                Title = title,
                TargetKeyword = targetKeyword,
                TargetLocation = pillar.TargetLocation,
                AnalysisRunId = pillar.AnalysisRunId,
                SerpKeyword = pillar.SerpKeyword,
                SiteProfileId = pillar.SiteProfileId,
                SiteFocusJson = pillar.SiteFocusJson,
                SiteFocusCapturedAt = pillar.SiteFocusCapturedAt,
                KeywordBundleJson = pillar.KeywordBundleJson,
                KeywordBundleCapturedAt = pillar.KeywordBundleCapturedAt,
                PublishSlug = publishSlug,
                SpokeSourceType = sourceType,
                SpokeSourcePhrase = phrase,
            },
            ct);

        if (!createResult.IsSuccess || createResult.Value is null)
            return Result<ContentSpokeSummary>.Failure(createResult.Error ?? "Failed to create spoke document");

        var shellHtml = BuildShellHtml(title);
        var wordCount = shellHtml.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var updated = await documentRepo.UpdateContentAsync(
            createResult.Value.Id,
            new UpdateContentRequest
            {
                ContentHtml = shellHtml,
                Title = title,
                TargetKeyword = targetKeyword,
                TargetLocation = pillar.TargetLocation,
            },
            wordCount,
            ct);

        if (!updated.IsSuccess || updated.Value is null)
            return Result<ContentSpokeSummary>.Failure(updated.Error ?? "Failed to save spoke shell content");

        return Result<ContentSpokeSummary>.Success(ToSummary(updated.Value));
    }

    public async Task<Result<ContentSpokeSummary>> GenerateAsync(
        Guid userId,
        Guid pillarDocumentId,
        Guid spokeDocumentId,
        GenerateContentSpokeRequest? request,
        CancellationToken ct = default)
    {
        var pillarResult = await RequirePillarAsync(userId, pillarDocumentId, ct);
        if (!pillarResult.IsSuccess || pillarResult.Value is null)
            return Result<ContentSpokeSummary>.Failure(pillarResult.Error ?? "Access denied");

        var pillar = pillarResult.Value;
        if (string.IsNullOrWhiteSpace(pillar.ContentHtml) || pillar.ContentHtml.Length < 200)
        {
            return Result<ContentSpokeSummary>.Failure(
                "Write or generate the pillar article before generating cluster spokes.");
        }

        var spokeAccess = await documents.EnsureAccessAsync(userId, spokeDocumentId, ct);
        if (!spokeAccess.IsSuccess || spokeAccess.Value is null)
            return Result<ContentSpokeSummary>.Failure(spokeAccess.Error ?? "Access denied");

        var spoke = spokeAccess.Value;
        if (!string.Equals(spoke.DocumentKind, ContentDocumentKinds.Spoke, StringComparison.OrdinalIgnoreCase))
            return Result<ContentSpokeSummary>.Failure("Document is not a cluster spoke.");

        if (spoke.ParentDocumentId != pillarDocumentId)
            return Result<ContentSpokeSummary>.Failure("Spoke does not belong to this pillar.");

        var sourcePhrase = spoke.SpokeSourcePhrase?.Trim();
        if (string.IsNullOrWhiteSpace(sourcePhrase))
            return Result<ContentSpokeSummary>.Failure("Spoke is missing its source phrase.");

        var spokeType = ResolveSpokeType(request?.SpokeType, spoke.SpokeSourceType);
        var pillarBackLink = ContentPublishPathResolver.ResolveRelativePath(pillar.PublishSlug) ?? string.Empty;
        var businessContext = SiteWritingFocusSerializer.TryDeserialize(pillar.SiteFocusJson) is { } focus
            ? SiteWritingFocusSerializer.ToBusinessContext(focus)
            : null;

        var draft = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentBlogSpokePromptBuilder.BuildClusterSpokeSystemPrompt(pillarBackLink),
            UserPrompt = ContentBlogSpokePromptBuilder.BuildClusterSpokeUserPrompt(
                pillar.Title,
                pillar.TargetKeyword,
                spokeType,
                sourcePhrase,
                spoke.Title,
                spoke.TargetKeyword,
                businessContext,
                pillar.ContentHtml),
            MaxTokens = 8192,
            Temperature = 0.7,
        }, ct);

        if (!draft.IsSuccess || draft.Value is null)
            return Result<ContentSpokeSummary>.Failure(draft.Error ?? "Spoke generation failed");

        var spokeHtml = AiHtmlSanitizer.ToHtmlFragment(draft.Value.Content);

        var metaResponse = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt = ContentBlogSpokePromptBuilder.BuildMetadataSystemPrompt(),
            UserPrompt = ContentBlogSpokePromptBuilder.BuildMetadataUserPrompt(
                spokeType, pillar.TargetKeyword, spokeHtml),
            MaxTokens = 512,
            Temperature = 0.3,
        }, ct);

        if (!metaResponse.IsSuccess || metaResponse.Value is null)
            return Result<ContentSpokeSummary>.Failure(metaResponse.Error ?? "Spoke metadata generation failed");

        if (!TryParseMetadata(
                metaResponse.Value.Content,
                out var title,
                out _,
                out var keyword,
                out _,
                out _))
        {
            return Result<ContentSpokeSummary>.Failure("Could not parse spoke metadata JSON from AI response.");
        }

        var validation = ContentBlogSpokeValidator.Validate(
            pillar.TargetKeyword,
            new ContentBlogSpoke
            {
                Title = title,
                Slug = spoke.PublishSlug ?? "spoke",
                PrimaryKeyword = string.IsNullOrWhiteSpace(keyword) ? spoke.TargetKeyword : keyword,
                SpokeType = spokeType,
                ContentHtml = spokeHtml,
            });

        if (!validation.IsValid)
            return Result<ContentSpokeSummary>.Failure(string.Join(' ', validation.Errors));

        var primaryKeyword = string.IsNullOrWhiteSpace(keyword) ? spoke.TargetKeyword : keyword.Trim();
        var wordCount = spokeHtml.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var updated = await documentRepo.UpdateContentAsync(
            spoke.Id,
            new UpdateContentRequest
            {
                ContentHtml = spokeHtml,
                Title = title,
                TargetKeyword = primaryKeyword,
                TargetLocation = spoke.TargetLocation,
            },
            wordCount,
            ct);

        if (!updated.IsSuccess || updated.Value is null)
            return Result<ContentSpokeSummary>.Failure(updated.Error ?? "Failed to save generated spoke content");

        var statusUpdated = await documentRepo.UpdateStatusAsync(
            spoke.Id,
            SpokeLinkStatuses.BodyGenerated,
            ct);

        if (!statusUpdated.IsSuccess || statusUpdated.Value is null)
            return Result<ContentSpokeSummary>.Failure(statusUpdated.Error ?? "Failed to update spoke status");

        return Result<ContentSpokeSummary>.Success(ToSummary(statusUpdated.Value));
    }

    private async Task<Result<SeoContentDocument>> RequirePillarAsync(
        Guid userId, Guid documentId, CancellationToken ct)
    {
        var access = await documents.EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return access;

        if (string.Equals(access.Value.DocumentKind, ContentDocumentKinds.Spoke, StringComparison.OrdinalIgnoreCase))
        {
            return Result<SeoContentDocument>.Failure(
                "Spokes can only be created from a pillar document.");
        }

        return access;
    }

    private static string ResolveSpokeType(string? requestedType, string? sourceType)
    {
        if (!string.IsNullOrWhiteSpace(requestedType))
            return requestedType.Trim();

        return string.Equals(sourceType, SpokeSourceTypes.Paa, StringComparison.OrdinalIgnoreCase)
            ? "guide"
            : "comparison";
    }

    private static ContentSpokeSummary ToSummary(SeoContentDocument doc) => new()
    {
        Id = doc.Id,
        Title = doc.Title,
        TargetKeyword = doc.TargetKeyword,
        PublishSlug = doc.PublishSlug,
        SpokeSourcePhrase = doc.SpokeSourcePhrase,
        SpokeSourceType = doc.SpokeSourceType,
        Status = doc.Status,
        WordCount = doc.WordCount,
        UpdatedAt = doc.UpdatedAt,
    };

    private static string BuildShellHtml(string title) =>
        $"<h1>{WebUtility.HtmlEncode(title)}</h1><p>Spoke draft shell. Generate full content in a later step.</p>";

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
