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
    ContentWriterHandoffService handoffService) : IContentDocumentService
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

        if (hasRun)
        {
            var handoff = await handoffService.ValidateAsync(
                request.AnalysisRunId!.Value,
                request.TargetKeyword,
                ct);
            if (!handoff.IsSuccess || handoff.Value is null)
                return Result<SeoContentDocument>.Failure(handoff.Error ?? "Site Analyzer research is not ready");

            var resolved = handoff.Value;
            SeoProject? projectValue = null;

            if (resolved.GeekSeoProjectId is Guid linkedProjectId && linkedProjectId != Guid.Empty)
            {
                var byId = await projects.GetByIdAsync(linkedProjectId, ct);
                if (byId.IsSuccess && byId.Value is not null && byId.Value.UserId == userId)
                    projectValue = byId.Value;
            }

            if (projectValue is null)
            {
                var userProjects = await projects.ListByUserAsync(userId, ct);
                projectValue = userProjects.IsSuccess ? userProjects.Value?.FirstOrDefault() : null;
            }

            if (projectValue is null)
                return Result<SeoContentDocument>.Failure("No Geek-SEO project found for this user. Create a project first.");

            var createRequest = request with
            {
                ProjectId = projectValue.Id,
                TargetLocation = ResolveTargetLocation(request.TargetLocation, projectValue.DefaultLocation),
                TargetKeyword = resolved.TargetKeyword,
                SerpKeyword = resolved.SerpKeyword,
                AnalysisRunId = resolved.AnalysisRunId,
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

        var handoff = await handoffService.ValidateAsync(analysisRunId, targetKeyword, ct);
        if (!handoff.IsSuccess || handoff.Value is null)
            return Result<SeoContentDocument>.Failure(handoff.Error ?? "Site Analyzer research is not ready");

        var resolved = handoff.Value;
        if (resolved.GeekSeoProjectId is Guid linkedId && linkedId != Guid.Empty && doc.ProjectId != linkedId)
            return Result<SeoContentDocument>.Failure("Document project does not match analysis run link.");

        return await documents.AttachAnalysisRunAsync(
            documentId,
            resolved.AnalysisRunId,
            resolved.TargetKeyword,
            resolved.SerpKeyword,
            siteProfileId ?? Guid.Empty,
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
