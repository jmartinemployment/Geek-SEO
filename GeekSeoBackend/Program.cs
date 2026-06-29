using DotNetEnv;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.Endpoints;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Hubs;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Middleware;
using GeekSeoBackend.Providers.Seo;
using GeekSeoBackend.Services;
using GeekSeoBackend.Workers;
using Microsoft.AspNetCore.Diagnostics;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment())
{
    ValidateRequiredGoogleOAuthEnv();
}

builder.Services.AddControllers()
    .AddSiteAnalyzer2Controllers();
builder.Services.AddSiteAnalyzer2Backend(builder.Configuration);

PlaywrightBrowserHolder? playwrightHolder = null;
var disablePlaywright = string.Equals(
    Environment.GetEnvironmentVariable("DISABLE_PLAYWRIGHT"), "true", StringComparison.OrdinalIgnoreCase);
if (!disablePlaywright)
{
    try
    {
        playwrightHolder = new PlaywrightBrowserHolder();
        await playwrightHolder.InitializeAsync();
        builder.Services.AddSingleton(playwrightHolder);
    }
    catch (Exception ex)
    {
        builder.Logging.AddConsole();
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
        logger.LogWarning(ex, "Playwright unavailable; competitor crawl disabled (NoOpCrawlerProvider).");
    }
}

builder.Services.AddGeekSeoBackend(builder.Configuration, playwrightHolder);
builder.Services.AddHostedService<FullArticleJobWorker>();
builder.Services.AddHostedService<BulkArticleJobWorker>();
builder.Services.AddHostedService<NicheAnalysisJobWorker>();
builder.Services.AddHostedService<UrlResearchJobWorker>();
builder.Services.AddHostedService<NicheEnrichmentJobWorker>();
builder.Services.AddHostedService<ContentDraftJobWorker>();
builder.Services.AddHostedService<ApplySourcesJobWorker>();
builder.Services.AddHostedService<SeoMaintenanceWorker>();

var corsOrigins = CorsOriginParser.GetAllowedOrigins();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

var gatewayUrl = Environment.GetEnvironmentVariable("GEEK_API_URL")
    ?? Environment.GetEnvironmentVariable("DATA_GATEWAY_URL")
    ?? (builder.Environment.IsDevelopment() ? "http://localhost:5272" : string.Empty);

if (string.IsNullOrWhiteSpace(gatewayUrl))
{
    throw new InvalidOperationException(
        "GEEK_API_URL is required. GeekRepository (REPO_URL) is only configured on GeekAPI.");
}

builder.Services.AddTransient<GeekDataGatewayHandler>();
builder.Services.AddHttpClient(GeekDataGateway.HttpClientName, client =>
{
    client.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
    // Large niche profiles (60+ pillars, step log, fusion snapshot) can exceed 60s on read/write.
    client.Timeout = TimeSpan.FromSeconds(180);
}).AddHttpMessageHandler<GeekDataGatewayHandler>();

var app = builder.Build();

if (SiteAnalyzer2BackendExtensions.IsEnabled(app.Configuration))
    await app.MigrateSiteAnalyzer2Async();

app.Lifetime.ApplicationStopping.Register(() =>
{
    if (playwrightHolder is not null)
        _ = playwrightHolder.DisposeAsync();
});

app.Logger.LogInformation("CORS origins: {Origins}", string.Join(", ", corsOrigins));
app.Logger.LogInformation("Data gateway: {Url} (providers run on GeekSeoBackend)", gatewayUrl);

app.UseCors();
app.UseMiddleware<PublicRateLimitMiddleware>();
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (ex is UnauthorizedAccessException)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
        return;
    }

    if (ex is not null)
    {
        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GeekSeoBackend.UnhandledException");
        logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
    }

    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred. Please try again." });
}));
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SeoFeatureGateMiddleware>();
app.UseMiddleware<SeoUsageGateMiddleware>();
app.MapControllers();

if (SiteAnalyzer2BackendExtensions.IsEnabled(app.Configuration))
    app.MapSa2CompetitorCrawlProgressStream();

app.MapHub<SeoContentScoringHub>("/hubs/seo-scoring");

var port = Environment.GetEnvironmentVariable("PORT") ?? "5051";
app.Run($"http://0.0.0.0:{port}");

static void ValidateRequiredGoogleOAuthEnv()
{
    var required = new[] { "GOOGLE_CLIENT_ID", "GOOGLE_CLIENT_SECRET", "GOOGLE_REDIRECT_URI" };
    var missing = required
        .Where(key => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        .ToArray();
    if (missing.Length > 0)
    {
        throw new InvalidOperationException(
            $"Missing required Google OAuth environment variables: {string.Join(", ", missing)}");
    }
}

public partial class Program;
