using SiteAnalyzer2.Services.Pipeline;

namespace SiteAnalyzer2.Api.HostedServices;

/// <summary>
/// Production/external Api deploys should not retain Chrome SERP HTML saves or *_files asset folders.
/// </summary>
public sealed class SerpFixtureCleanupHostedService(
    IWebHostEnvironment environment,
    ILogger<SerpFixtureCleanupHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldClean())
            return Task.CompletedTask;

        var removed = SerpFixtureFileCleanup.CleanUnderContentRoot(environment.ContentRootPath);
        if (removed > 0)
        {
            logger.LogInformation(
                "Removed {Count} SERP fixture file(s)/folder(s) from {Path} (production/external cleanup).",
                removed,
                Path.Combine(environment.ContentRootPath, "fixtures", "serp"));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private bool ShouldClean() =>
        SerpExecutionConfiguration.IsExternal
        || environment.IsProduction();
}
