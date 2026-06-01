using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Workers;

public sealed class SeoMaintenanceWorker(
    IServiceProvider services,
    WorkerUserContext workerUser,
    ILogger<SeoMaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SeoMaintenanceWorker iteration failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task RunMaintenanceAsync(CancellationToken ct)
    {
        if (!TryResolveWorkerUserId(out var serviceUserId))
        {
            logger.LogDebug("WORKER_SERVICE_USER_ID not set — skipping scheduled maintenance");
            return;
        }

        workerUser.UserId = serviceUserId;

        using var scope = services.CreateScope();
        var topicalMaps = scope.ServiceProvider.GetRequiredService<ITopicalMapRepository>();
        var topicalMapService = scope.ServiceProvider.GetRequiredService<TopicalMapService>();
        var publishedPages = scope.ServiceProvider.GetRequiredService<IPublishedPageRepository>();
        var auditService = scope.ServiceProvider.GetRequiredService<PublishedContentAuditService>();
        var geoTracking = scope.ServiceProvider.GetRequiredService<IGeoTrackingRepository>();
        var geoService = scope.ServiceProvider.GetRequiredService<GeoVisibilityService>();
        var contentGuard = scope.ServiceProvider.GetRequiredService<IContentGuardRepository>();
        var guardService = scope.ServiceProvider.GetRequiredService<ContentGuardService>();
        var rankRepo = scope.ServiceProvider.GetRequiredService<IRankTrackingRepository>();
        var rankTracking = scope.ServiceProvider.GetRequiredService<RankTrackingService>();
        var projects = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

        var dueMaps = await topicalMaps.ListDueForRefreshAsync(5, ct);
        if (dueMaps.IsSuccess && dueMaps.Value is not null)
        {
            foreach (var item in dueMaps.Value)
            {
                workerUser.UserId = item.UserId;
                try
                {
                    await topicalMapService.GenerateAsync(item.UserId, item.ProjectId, force: false, ct);
                    logger.LogInformation("Refreshed topical map for project {ProjectId}", item.ProjectId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Topical map refresh failed for {ProjectId}", item.ProjectId);
                }
            }
        }

        workerUser.UserId = serviceUserId;
        var duePages = await publishedPages.ListDueForSnapshotAsync(20, ct);
        if (duePages.IsSuccess && duePages.Value is not null)
        {
            foreach (var group in duePages.Value.GroupBy(p => p.ProjectId))
            {
                workerUser.UserId = group.First().UserId;
                try
                {
                    await auditService.SnapshotPublishedPagesAsync(group.First().UserId, group.Key, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Published page snapshot failed for {ProjectId}", group.Key);
                }
            }
        }

        workerUser.UserId = serviceUserId;
        if (DateTime.UtcNow.Hour == 5)
        {
            var geoQueries = await geoTracking.ListEnabledQueriesAsync(50, ct);
            if (geoQueries.IsSuccess && geoQueries.Value is not null)
            {
                foreach (var query in geoQueries.Value)
                {
                    workerUser.UserId = query.UserId;
                    try
                    {
                        await geoService.ProbeTrackedQueryAsync(query, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "GEO probe failed for query {QueryId}", query.QueryId);
                    }
                }
            }

            workerUser.UserId = serviceUserId;
            var guardProjects = await contentGuard.ListProjectsForDailyScanAsync(20, ct);
            if (guardProjects.IsSuccess && guardProjects.Value is not null)
            {
                foreach (var project in guardProjects.Value)
                {
                    workerUser.UserId = project.UserId;
                    try
                    {
                        await guardService.ScanProjectAsync(project.UserId, project.ProjectId, project.AutoPatch, ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Content Guard scan failed for {ProjectId}", project.ProjectId);
                    }
                }
            }

            workerUser.UserId = serviceUserId;
            var projectsWithKeywords = await rankRepo.ListProjectsWithKeywordsAsync(100, ct);
            if (projectsWithKeywords.IsSuccess && projectsWithKeywords.Value is not null)
            {
                foreach (var projectId in projectsWithKeywords.Value)
                {
                    try
                    {
                        var projectResult = await projects.GetByIdAsync(projectId, serviceUserId, ct);
                        if (projectResult.IsSuccess && projectResult.Value is not null)
                        {
                            workerUser.UserId = projectResult.Value.UserId;
                            await rankTracking.SnapshotProjectRanksAsync(projectId, ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Rank snapshot failed for project {ProjectId}", projectId);
                    }
                }
            }
        }

        workerUser.UserId = Guid.Empty;
    }

    private static bool TryResolveWorkerUserId(out Guid userId)
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
