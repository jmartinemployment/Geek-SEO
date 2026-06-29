using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentBlogSpokeMigrator(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo) : IContentBlogSpokeMigrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<Result<Guid?>> EnsureMigratedChildAsync(
        Guid userId,
        Guid pillarDocumentId,
        CancellationToken ct = default)
    {
        var access = await documents.EnsureAccessAsync(userId, pillarDocumentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return Result<Guid?>.Failure(access.Error ?? "Access denied");

        var pillar = access.Value;
        if (string.Equals(pillar.DocumentKind, ContentDocumentKinds.Spoke, StringComparison.OrdinalIgnoreCase))
            return Result<Guid?>.Success(null);

        var spoke = Parse(pillar.BlogSpokeJson);
        if (spoke is null)
            return Result<Guid?>.Success(null);

        var projectDocs = await documentRepo.GetByProjectAsync(pillar.ProjectId, ct);
        if (!projectDocs.IsSuccess || projectDocs.Value is null)
            return Result<Guid?>.Failure(projectDocs.Error ?? "Failed to load project documents");

        var existingChild = projectDocs.Value
            .Where(d => d.ParentDocumentId == pillarDocumentId)
            .OrderBy(d => d.CreatedAt)
            .FirstOrDefault();
        if (existingChild is not null)
            return Result<Guid?>.Success(existingChild.Id);

        var validation = ContentBlogSpokeValidator.Validate(pillar.TargetKeyword, spoke);
        if (!validation.IsValid)
            return Result<Guid?>.Failure(string.Join(' ', validation.Errors));

        var existingSlugs = projectDocs.Value
            .Select(d => d.PublishSlug)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToList();

        var baseSlug = ContentPublishSlug.NormalizeFromPhrase(
            string.IsNullOrWhiteSpace(spoke.Slug) ? spoke.Title : spoke.Slug);
        if (string.IsNullOrWhiteSpace(baseSlug))
            return Result<Guid?>.Failure("Could not derive a publish slug for the migrated spoke.");

        var publishSlug = ContentPublishSlug.AllocateUnique(baseSlug, existingSlugs);
        var sourcePhrase = string.IsNullOrWhiteSpace(spoke.PrimaryKeyword) ? spoke.Title : spoke.PrimaryKeyword;
        var wordCount = spoke.ContentHtml.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        var migrated = await documentRepo.MigrateBlogSpokeChildIfAbsentAsync(
            userId,
            pillarDocumentId,
            new MigrateBlogSpokeChildPayload
            {
                Child = new CreateContentDocumentRequest
                {
                    ProjectId = pillar.ProjectId,
                    ParentDocumentId = pillar.Id,
                    DocumentKind = ContentDocumentKinds.Spoke,
                    Title = spoke.Title,
                    TargetKeyword = spoke.PrimaryKeyword,
                    TargetLocation = pillar.TargetLocation,
                    AnalysisRunId = pillar.AnalysisRunId,
                    SerpKeyword = pillar.SerpKeyword,
                    SiteProfileId = pillar.SiteProfileId,
                    SiteFocusJson = pillar.SiteFocusJson,
                    SiteFocusCapturedAt = pillar.SiteFocusCapturedAt,
                    KeywordBundleJson = pillar.KeywordBundleJson,
                    KeywordBundleCapturedAt = pillar.KeywordBundleCapturedAt,
                    PublishSlug = publishSlug,
                    SpokeSourceType = SpokeSourceTypes.Migrated,
                    SpokeSourcePhrase = sourcePhrase.Trim(),
                },
                ContentHtml = spoke.ContentHtml,
                WordCount = wordCount,
                Status = SpokeLinkStatuses.BodyGenerated,
            },
            ct);

        if (!migrated.IsSuccess || migrated.Value is null)
            return Result<Guid?>.Failure(migrated.Error ?? "Failed to migrate blog spoke");

        return Result<Guid?>.Success(migrated.Value.Id);
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
}
