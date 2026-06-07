using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class ProjectService(IProjectRepository projects) : IProjectService
{
    public async Task<Result<IReadOnlyList<SeoProject>>> ListAsync(Guid userId, CancellationToken ct = default) =>
        await projects.ListByUserAsync(userId, ct);

    public async Task<Result<SeoProject>> GetAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var result = await projects.GetByIdAsync(projectId, ct);
        if (!result.IsSuccess || result.Value is null)
            return Result<SeoProject>.NotFound("Project not found");
        if (result.Value.UserId != userId)
            return Result<SeoProject>.Failure("Access denied");
        return result;
    }

    public Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default) =>
        projects.CreateAsync(userId, NormalizeCreate(request), ct);

    public async Task<Result<SeoProject>> UpdateAsync(
        Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var access = await GetAsync(userId, projectId, ct);
        if (!access.IsSuccess)
            return access;
        return await projects.UpdateAsync(projectId, NormalizeUpdate(request), ct);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var access = await GetAsync(userId, projectId, ct);
        if (!access.IsSuccess)
            return Result.Failure(access.Error ?? "Access denied");
        return await projects.DeleteAsync(projectId, ct);
    }

    public static CreateProjectRequest NormalizeCreate(CreateProjectRequest request) =>
        request with
        {
            BusinessAddress = NormalizeAddress(request.BusinessAddress),
            ServiceRadiusMiles = LocalServiceAreaDefaults.ClampRadiusMiles(request.ServiceRadiusMiles),
        };

    public static UpdateProjectRequest NormalizeUpdate(UpdateProjectRequest request) =>
        request with
        {
            BusinessAddress = request.BusinessAddress is null
                ? null
                : NormalizeAddress(request.BusinessAddress),
            ServiceRadiusMiles = request.ServiceRadiusMiles is int radius
                ? LocalServiceAreaDefaults.ClampRadiusMiles(radius)
                : request.ServiceRadiusMiles,
        };

    private static string? NormalizeAddress(string? address)
    {
        if (address is null)
            return null;
        var trimmed = address.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
