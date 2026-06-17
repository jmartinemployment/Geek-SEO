using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services.NicheExtraction;

namespace GeekSeoBackend.Services.LocalServiceArea;

public sealed class LocalSerpContextResolver(
    IProjectRepository projectRepo,
    IGeocodeService geocodeService,
    ILogger<LocalSerpContextResolver> logger) : ILocalSerpContextResolver
{
    public async Task<Result<LocalSerpContext>> ResolveAsync(Guid projectId, CancellationToken ct = default)
    {
        var projectResult = await projectRepo.GetByIdAsync(projectId, ct);
        if (!projectResult.IsSuccess || projectResult.Value is null)
            return Result<LocalSerpContext>.Failure(projectResult.Error ?? "Project not found.");

        return Result<LocalSerpContext>.Success(await ResolveFromProjectAsync(projectResult.Value, ct));
    }

    internal async Task<LocalSerpContext> ResolveFromProjectAsync(
        SeoProject project,
        CancellationToken ct)
    {
        var serpMarket = string.IsNullOrWhiteSpace(project.DefaultLocation)
            ? PillarDemandEnricher.NationalLocation
            : project.DefaultLocation.Trim();

        if (!project.LocalSeoEnabled)
            return new LocalSerpContext(PillarDemandEnricher.NationalLocation, null);

        var geocodeQuery = !string.IsNullOrWhiteSpace(project.BusinessAddress)
            ? project.BusinessAddress.Trim()
            : serpMarket.Equals(PillarDemandEnricher.NationalLocation, StringComparison.OrdinalIgnoreCase)
                ? null
                : serpMarket;

        if (geocodeQuery is null)
            return new LocalSerpContext(serpMarket, null);

        var geo = await geocodeService.GeocodeAsync(geocodeQuery, ct);
        if (!geo.IsSuccess || geo.Value is null)
        {
            logger.LogWarning(
                "Geocode failed for project {ProjectId} query '{Query}': {Error}. Local competitors will not be radius-filtered.",
                project.Id,
                geocodeQuery,
                geo.Error);
            return new LocalSerpContext(serpMarket, null);
        }

        var radius = LocalServiceAreaDefaults.ClampRadiusMiles(project.ServiceRadiusMiles);
        var area = new LocalServiceAreaContext(geo.Value.Latitude, geo.Value.Longitude, radius);
        logger.LogInformation(
            "Local service area for project {ProjectId}: center ({Lat},{Lng}), radius {RadiusMi} mi, SERP market '{Market}'",
            project.Id,
            geo.Value.Latitude,
            geo.Value.Longitude,
            radius,
            serpMarket);

        return new LocalSerpContext(serpMarket, area);
    }
}
