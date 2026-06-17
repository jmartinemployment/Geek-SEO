using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class UrlResearchService(IUrlResearchRepository research, IProjectRepository projects)
    : IUrlResearchService
{
    public async Task<Result<SeoUrlResearch>> CreateQueuedAsync(
        Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default)
    {
        var access = await EnsureProjectAccessAsync(userId, request.ProjectId, ct);
        if (!access.IsSuccess)
            return Result<SeoUrlResearch>.Failure(access.Error ?? "Access denied");

        return await research.CreateQueuedAsync(userId, request, ct);
    }

    public async Task<Result<SeoUrlResearch>> GetFullAsync(
        Guid userId, Guid urlResearchId, CancellationToken ct = default)
    {
        var access = await EnsureResearchAccessAsync(userId, urlResearchId, ct);
        if (!access.IsSuccess)
            return access;

        return await research.GetFullAsync(urlResearchId, ct);
    }

    public async Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(
        Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var access = await EnsureProjectAccessAsync(userId, projectId, ct);
        if (!access.IsSuccess)
            return Result<IReadOnlyList<UrlResearchSummary>>.Failure(access.Error ?? "Access denied");

        return await research.ListSummaryByProjectAsync(projectId, ct);
    }

    public async Task<Result<SeoUrlResearch>> PersistFullAsync(
        Guid userId, Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default)
    {
        var access = await EnsureResearchAccessAsync(userId, urlResearchId, ct);
        if (!access.IsSuccess)
            return access;

        return await research.PersistFullAsync(urlResearchId, body, ct);
    }

    public async Task<Result<SeoUrlResearch>> UpdateStatusAsync(
        Guid userId, Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default)
    {
        var access = await EnsureResearchAccessAsync(userId, urlResearchId, ct);
        if (!access.IsSuccess)
            return access;

        return await research.UpdateStatusAsync(urlResearchId, patch, ct);
    }

    private async Task<Result> EnsureProjectAccessAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await projects.GetByIdAsync(projectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            return Result.Failure("Access denied");
        return Result.Success();
    }

    private async Task<Result<SeoUrlResearch>> EnsureResearchAccessAsync(
        Guid userId, Guid urlResearchId, CancellationToken ct)
    {
        var row = await research.GetFullAsync(urlResearchId, ct);
        if (!row.IsSuccess || row.Value is null)
            return Result<SeoUrlResearch>.NotFound("Page research not found");
        if (row.Value.UserId != userId)
            return Result<SeoUrlResearch>.Failure("Access denied");

        var project = await projects.GetByIdAsync(row.Value.ProjectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            return Result<SeoUrlResearch>.Failure("Access denied");

        return row;
    }
}
