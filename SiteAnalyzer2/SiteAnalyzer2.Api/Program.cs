using SiteAnalyzer2.Api;
using SiteAnalyzer2.Api.Endpoints;
using SiteAnalyzer2.Api.Hubs;
using SiteAnalyzer2.Services.BusinessFocus;
using SiteAnalyzer2.Infrastructure;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Repositories;
using SiteAnalyzer2.Services;
using SiteAnalyzer2.Serp;

var builder = WebApplication.CreateBuilder(args);

var connectionString = DatabaseConnection.ResolveRequired(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
var corsOrigins = CorsConfiguration.ResolveAllowedOrigins();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddRepositories();
builder.Services.AddSerpProviders();
builder.Services.AddSiteAnalyzerServices();
builder.Services.AddScoped<SiteAnalyzer2.Services.IRunProgressNotifier, SignalRRunProgressNotifier>();
builder.Services.AddSingleton<SiteAnalyzer2.Api.Realtime.CrawlProgressBroadcaster>();
builder.Services.AddSingleton(_ => new SiteAnalyzer2.Api.Realtime.PostgresCompetitorCrawlNotifier(connectionString));
builder.Services.AddHostedService<SiteAnalyzer2.Api.HostedServices.SerpClaimTimeoutHostedService>();
builder.Services.AddHostedService<SiteAnalyzer2.Api.HostedServices.SerpFixtureCleanupHostedService>();
builder.Services.AddHostedService<SiteAnalyzer2.Api.HostedServices.CompetitorCrawlProgressRelayHostedService>();
builder.Services.AddHostedService(sp =>
    new SiteAnalyzer2.Api.HostedServices.PostgresCompetitorCrawlListenHostedService(
        connectionString,
        sp.GetRequiredService<SiteAnalyzer2.Api.Realtime.CrawlProgressBroadcaster>(),
        sp.GetRequiredService<ILogger<SiteAnalyzer2.Api.HostedServices.PostgresCompetitorCrawlListenHostedService>>()));

var app = builder.Build();

await app.Services.MigrateAndSeedAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    });
});

app.UseRouting();
app.UseCors();
app.MapGet("/", () => Results.Ok(new { service = "SiteAnalyzer API", health = "/health", config = "/config" }));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/config", () =>
{
    try
    {
        return Results.Ok(new { businessFocusProvider = BusinessFocusProviderConfiguration.ResolveProviderName() });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapControllers();
app.MapCompetitorCrawlProgressStream();
app.MapHub<RunProgressHub>("/hubs/run-progress");

app.Run();

public partial class Program;
