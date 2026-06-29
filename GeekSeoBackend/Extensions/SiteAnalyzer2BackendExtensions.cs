using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.HttpClients.Repo;
using GeekSeoBackend.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Api.Controllers;
using SiteAnalyzer2.Api.HostedServices;
using SiteAnalyzer2.Api.Realtime;
using SiteAnalyzer2.Infrastructure;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services;
using SiteAnalyzer2.Serp;

namespace GeekSeoBackend.Extensions;

public static class SiteAnalyzer2BackendExtensions
{
    public const string EnabledEnvVar = "SITE_ANALYZER2_DATABASE_URL";
    public const string InProcessReposEnvVar = "SA2_IN_PROCESS_REPOS";

    public static bool IsEnabled(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnabledEnvVar))
        || !string.IsNullOrWhiteSpace(configuration.GetConnectionString("SiteAnalyzer2"));

    public static bool UseInProcessRepos() =>
        string.Equals(
            Environment.GetEnvironmentVariable(InProcessReposEnvVar),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static IServiceCollection AddSiteAnalyzer2Backend(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
            return services;

        var connectionString = DatabaseConnection.ResolveRequired(configuration);

        services.AddInfrastructure(connectionString);
        services.AddRepositories();
        services.AddSerpProviders();
        services.AddSiteAnalyzerServices();
        services.AddScoped<IRunProgressNotifier, NoOpRunProgressNotifier>();
        services.AddSingleton<CrawlProgressBroadcaster>();
        services.AddSingleton(_ => new PostgresCompetitorCrawlNotifier(connectionString));
        services.AddHostedService<SerpFixtureCleanupHostedService>();

        var serpExecution = Environment.GetEnvironmentVariable("SERP_EXECUTION") ?? "manual";
        if (string.Equals(serpExecution, "external", StringComparison.OrdinalIgnoreCase))
            services.AddHostedService<SerpClaimTimeoutHostedService>();

        services.AddHostedService<CompetitorCrawlProgressRelayHostedService>();
        services.AddHostedService(sp =>
            new PostgresCompetitorCrawlListenHostedService(
                connectionString,
                sp.GetRequiredService<CrawlProgressBroadcaster>(),
                sp.GetRequiredService<ILogger<PostgresCompetitorCrawlListenHostedService>>()));

        if (UseInProcessRepos())
        {
            services.AddScoped<GeekSeo.Application.Interfaces.Seo.IAnalysisRunRepository, InProcessAnalysisRunRepository>();
            services.AddScoped<ISiteAnalyzer2SiteProfileRepository, InProcessSiteAnalyzer2SiteProfileRepository>();
        }

        return services;
    }

    public static IMvcBuilder AddSiteAnalyzer2Controllers(this IMvcBuilder mvcBuilder) =>
        mvcBuilder
            .AddApplicationPart(typeof(SitesController).Assembly)
            .AddMvcOptions(options => options.Conventions.Add(new SiteAnalyzer2RoutePrefixConvention()));

    public static async Task MigrateSiteAnalyzer2Async(this WebApplication app, CancellationToken ct = default)
    {
        if (!IsEnabled(app.Configuration))
            return;

        await app.Services.MigrateAndSeedAsync(ct);
        app.Logger.LogInformation("Site Analyzer 2 (sa2) migrations applied.");
    }
}
