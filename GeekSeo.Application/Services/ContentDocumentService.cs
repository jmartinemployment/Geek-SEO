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
    private const string DefaultTargetLocation = "United States";

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
        var hasRun = request.AnalysisRunId is { } analysisRunId && analysisRunId != Guid.Empty;
        var hasProfile = request.SiteProfileId is { } siteProfileId && siteProfileId != Guid.Empty;

        if (hasRun != hasProfile)
        {
            return Result<SeoContentDocument>.Failure(
                "analysisRunId and site_profile are both required for Content Writing handoff.");
        }

        if (hasRun)
        {
            if (request.ProjectId != Guid.Empty)
            {
                return Result<SeoContentDocument>.Failure(
                    "projectId is not accepted on SA2 handoff. Open Content Writing from Site Analyzer with site_profile.");
            }

            var frozen = await handoff.FreezeAsync(
                request.AnalysisRunId!.Value,
                request.SiteProfileId!.Value,
                request.TargetKeyword,
                request.TargetLocation,
                ct);
            if (!frozen.IsSuccess || frozen.Value is null)
                return Result<SeoContentDocument>.Failure(frozen.Error ?? "Failed to freeze research handoff");

            var bundle = frozen.Value;
            var project = await projects.GetByIdAsync(bundle.GeekSeoProjectId, ct);
            if (!project.IsSuccess || project.Value is null || project.Value.UserId != userId)
                return Result<SeoContentDocument>.Failure("Access denied");

            var createRequest = request with
            {
                ProjectId = bundle.GeekSeoProjectId,
                TargetLocation = ResolveTargetLocation(request.TargetLocation, project.Value.DefaultLocation),
                TargetKeyword = bundle.TargetKeyword,
                SerpKeyword = bundle.SerpKeyword,
                AnalysisRunId = bundle.AnalysisRunId,
                SiteProfileId = bundle.SiteProfileId,
                SiteFocusJson = bundle.SiteFocusJson,
                SiteFocusCapturedAt = bundle.SiteFocusCapturedAt,
                KeywordBundleJson = bundle.KeywordBundleJson,
                KeywordBundleCapturedAt = bundle.KeywordBundleCapturedAt,
            };

            return await documents.CreateAsync(userId, createRequest, ct);
        }

        if (request.ProjectId == Guid.Empty)
            return Result<SeoContentDocument>.Failure("projectId is required.");

        var standaloneProject = await projects.GetByIdAsync(request.ProjectId, ct);
        if (!standaloneProject.IsSuccess || standaloneProject.Value is null || standaloneProject.Value.UserId != userId)
            return Result<SeoContentDocument>.Failure("Access denied");

        return await documents.CreateAsync(userId, request, ct);
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
            "urlResearchId handoff was removed. Open Content Writing from Site Analyzer with analysisRunId and site_profile."));

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
            analysisRunId,
            profileId,
            targetKeyword,
            doc.TargetLocation,
            ct);
        if (!frozen.IsSuccess || frozen.Value is null)
            return Result<SeoContentDocument>.Failure(frozen.Error ?? "Failed to freeze research handoff");

        var bundle = frozen.Value;
        if (doc.ProjectId != bundle.GeekSeoProjectId)
            return Result<SeoContentDocument>.Failure("Document project does not match site_profile link.");

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

    private static string ResolveTargetLocation(string requested, string? projectDefault)
    {
        if (!string.IsNullOrWhiteSpace(projectDefault) &&
            (string.IsNullOrWhiteSpace(requested) || requested == DefaultTargetLocation))
        {
            return projectDefault;
        }

        return string.IsNullOrWhiteSpace(requested) ? DefaultTargetLocation : requested;
    }

    private static int CountWords(string html)
    {
        var text = HtmlTagRegex().Replace(html, " ");
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.None)]
    private static partial Regex HtmlTagRegex();
}
