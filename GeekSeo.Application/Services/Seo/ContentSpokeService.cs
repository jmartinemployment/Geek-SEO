using System.Net;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentSpokeService(
    IContentDocumentService documents,
    IContentDocumentRepository documentRepo) : IContentSpokeService
{
    public async Task<Result<IReadOnlyList<ContentSpokeSummary>>> ListAsync(
        Guid userId,
        Guid pillarDocumentId,
        CancellationToken ct = default)
    {
        var pillar = await RequirePillarAsync(userId, pillarDocumentId, ct);
        if (!pillar.IsSuccess || pillar.Value is null)
            return Result<IReadOnlyList<ContentSpokeSummary>>.Failure(pillar.Error ?? "Access denied");

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
}
