using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Infrastructure;
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

    public async Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default)
    {
        var normalized = NormalizeCreate(request);
        var duplicate = await EnsureUniqueUrlAsync(userId, normalized.Url, excludeProjectId: null, ct);
        if (!duplicate.IsSuccess)
            return Result<SeoProject>.Failure(duplicate.Error ?? "A project for this website already exists.");
        return await projects.CreateAsync(userId, normalized, ct);
    }

    public async Task<Result<SeoProject>> UpdateAsync(
        Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var access = await GetAsync(userId, projectId, ct);
        if (!access.IsSuccess)
            return access;
        var normalized = NormalizeUpdate(request);
        if (!string.IsNullOrWhiteSpace(normalized.Url))
        {
            var duplicate = await EnsureUniqueUrlAsync(userId, normalized.Url!, projectId, ct);
            if (!duplicate.IsSuccess)
                return Result<SeoProject>.Failure(duplicate.Error ?? "A project for this website already exists.");
        }

        return await projects.UpdateAsync(projectId, normalized, ct);
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
            Url = SeoSiteUrlNormalizer.Normalize(request.Url),
            BusinessAddress = NormalizeAddress(request.BusinessAddress),
            ServiceRadiusMiles = LocalServiceAreaDefaults.ClampRadiusMiles(request.ServiceRadiusMiles),
        };

    public static UpdateProjectRequest NormalizeUpdate(UpdateProjectRequest request) =>
        request with
        {
            Url = request.Url is null
                ? null
                : SeoSiteUrlNormalizer.Normalize(request.Url),
            BusinessAddress = request.BusinessAddress is null
                ? null
                : NormalizeAddress(request.BusinessAddress),
            ServiceRadiusMiles = request.ServiceRadiusMiles is int radius
                ? LocalServiceAreaDefaults.ClampRadiusMiles(radius)
                : request.ServiceRadiusMiles,
        };

    private async Task<Result> EnsureUniqueUrlAsync(
        Guid userId,
        string normalizedUrl,
        Guid? excludeProjectId,
        CancellationToken ct)
    {
        var existing = await projects.ListByUserAsync(userId, ct);
        if (!existing.IsSuccess || existing.Value is null)
            return Result.Failure(existing.Error ?? "Could not validate project URL uniqueness.");

        var duplicate = existing.Value.Any(project =>
            project.Id != excludeProjectId
            && string.Equals(
                SeoSiteUrlNormalizer.Normalize(project.Url),
                normalizedUrl,
                StringComparison.OrdinalIgnoreCase));

        return duplicate
            ? Result.Failure("A project for this website already exists.")
            : Result.Success();
    }

    private static string? NormalizeAddress(string? address)
    {
        if (address is null)
            return null;
        var trimmed = address.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
