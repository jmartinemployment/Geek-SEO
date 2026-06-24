using System.Text.RegularExpressions;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed partial class ContentDocumentService(
    IContentDocumentRepository documents,
    IProjectRepository projects,
    ContentWriterHandoffService handoff) : IContentDocumentService
{
    public async Task<Result<SeoContentDocument>> EnsureAccessAsync(
        Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var docResult = await documents.GetByIdAsync(documentId, ct);
        if (!docResult.IsSuccess || docResult.Value is null)
            return Result<SeoContentDocument>.NotFound("Document not found");
        if (docResult.Value.UserId != userId)
            return Result<SeoContentDocument>.Failure("Access denied");
        return docResult;
    }

    public async Task<Result<IReadOnlyList<SeoContentDocument>>> ListByProjectAsync(
        Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, ct);
        if (!project.IsSuccess || project.Value is null || project.Value.UserId != userId)
            return Result<IReadOnlyList<SeoContentDocument>>.Failure("Access denied");
        return await documents.GetByProjectAsync(projectId, ct);
    }

    public Task<Result<SeoContentDocument>> GetAsync(Guid userId, Guid documentId, CancellationToken ct = default) =>
        EnsureAccessAsync(userId, documentId, ct);

    public async Task<Result<SeoContentDocument>> CreateAsync(
        Guid userId, CreateContentDocumentRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(request.ProjectId, ct);
        if (!project.IsSuccess || project.Value is null || project.Value.UserId != userId)
            return Result<SeoContentDocument>.Failure("Access denied");

        var createRequest = request;
        if (request.AnalysisRunId is { } analysisRunId)
        {
            if (request.SiteProfileId is not { } siteProfileId || siteProfileId == Guid.Empty)
                return Result<SeoContentDocument>.Failure("site_profile is required for research-backed content.");

            var frozen = await handoff.FreezeAsync(
                request.ProjectId,
                analysisRunId,
                siteProfileId,
                request.TargetKeyword,
                request.TargetLocation,
                ct);
            if (!frozen.IsSuccess || frozen.Value is null)
                return Result<SeoContentDocument>.Failure(frozen.Error ?? "Failed to freeze research handoff");

            var bundle = frozen.Value;
            createRequest = request with
            {
                TargetKeyword = bundle.TargetKeyword,
                SerpKeyword = bundle.SerpKeyword,
                AnalysisRunId = bundle.AnalysisRunId,
                SiteProfileId = bundle.SiteProfileId,
                SiteFocusJson = bundle.SiteFocusJson,
                SiteFocusCapturedAt = bundle.SiteFocusCapturedAt,
                KeywordBundleJson = bundle.KeywordBundleJson,
                KeywordBundleCapturedAt = bundle.KeywordBundleCapturedAt,
            };
        }

        return await documents.CreateAsync(userId, createRequest, ct);
    }

    public async Task<Result<SeoContentDocument>> UpdateContentAsync(
        Guid userId, Guid documentId, UpdateContentRequest request, CancellationToken ct = default)
    {
        var access = await EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess)
            return access;
        var wordCount = CountWords(request.ContentHtml);
        return await documents.UpdateContentAsync(documentId, request, wordCount, ct);
    }

    public async Task<Result<SeoContentDocument>> UpdateStatusAsync(
        Guid userId, Guid documentId, string status, CancellationToken ct = default)
    {
        var access = await EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess)
            return access;
        return await documents.UpdateStatusAsync(documentId, status, ct);
    }

    public Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
        Guid userId, Guid documentId, Guid urlResearchId, CancellationToken ct = default) =>
        Task.FromResult(Result<SeoContentDocument>.Failure(
            "Content Writing requires an analysis run. Use analysisRunId instead of urlResearchId."));

    public async Task<Result<SeoContentDocument>> AttachAnalysisRunAsync(
        Guid userId,
        Guid documentId,
        Guid analysisRunId,
        string targetKeyword,
        string serpKeyword,
        Guid? siteProfileId = null,
        CancellationToken ct = default)
    {
        var access = await EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess || access.Value is null)
            return access;

        var doc = access.Value;
        if (siteProfileId is not { } profileId || profileId == Guid.Empty)
            return Result<SeoContentDocument>.Failure("site_profile is required for research-backed content.");

        var frozen = await handoff.FreezeAsync(
            doc.ProjectId,
            analysisRunId,
            profileId,
            targetKeyword,
            doc.TargetLocation,
            ct);
        if (!frozen.IsSuccess || frozen.Value is null)
            return Result<SeoContentDocument>.Failure(frozen.Error ?? "Failed to freeze research handoff");

        var bundle = frozen.Value;
        return await documents.AttachAnalysisRunAsync(
            documentId,
            bundle.AnalysisRunId,
            bundle.TargetKeyword,
            bundle.SerpKeyword,
            bundle.SiteProfileId,
            bundle.SiteFocusJson,
            bundle.SiteFocusCapturedAt,
            bundle.KeywordBundleJson,
            bundle.KeywordBundleCapturedAt,
            ct);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var access = await EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess)
            return Result.Failure(access.Error ?? "Access denied");
        return await documents.DeleteAsync(documentId, ct);
    }

    private static int CountWords(string html)
    {
        var text = HtmlTagRegex().Replace(html, " ");
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.None)]
    private static partial Regex HtmlTagRegex();
}
