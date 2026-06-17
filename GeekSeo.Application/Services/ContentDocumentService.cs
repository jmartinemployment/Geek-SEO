using System.Text.RegularExpressions;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed partial class ContentDocumentService(
    IContentDocumentRepository documents,
    IProjectRepository projects,
    IUrlResearchService urlResearch) : IContentDocumentService
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
        if (request.UrlResearchId is { } urlResearchId)
        {
            var research = await urlResearch.GetFullAsync(userId, urlResearchId, ct);
            if (!research.IsSuccess || research.Value is null)
                return Result<SeoContentDocument>.Failure(research.Error ?? "Page research not found");

            var validation = ResearchBackedWriteGate.ValidateResearchForProject(request.ProjectId, research.Value);
            if (!validation.IsSuccess)
                return Result<SeoContentDocument>.Failure(validation.Error ?? "Invalid page research");

            createRequest = request with
            {
                TargetKeyword = string.IsNullOrWhiteSpace(request.TargetKeyword)
                    ? research.Value.DerivedKeyword
                    : request.TargetKeyword,
                TargetLocation = string.IsNullOrWhiteSpace(request.TargetLocation) ||
                                 string.Equals(request.TargetLocation, "United States", StringComparison.Ordinal)
                    ? research.Value.SearchLocation
                    : request.TargetLocation,
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

    public async Task<Result<SeoContentDocument>> AttachUrlResearchAsync(
        Guid userId, Guid documentId, Guid urlResearchId, CancellationToken ct = default)
    {
        var access = await EnsureAccessAsync(userId, documentId, ct);
        if (!access.IsSuccess)
            return access;
        return await documents.AttachUrlResearchAsync(documentId, urlResearchId, ct);
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
