using DotNetEnv;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Hubs;
using GeekSeoBackend.Middleware;
using GeekSeoBackend.Services;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddGeekSeoBackend(builder.Configuration);

var corsOrigins = CorsOriginParser.GetAllowedOrigins();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));

var repoUrl = Environment.GetEnvironmentVariable("REPO_URL")
    ?? Environment.GetEnvironmentVariable("GEEK_REPO_URL")
    ?? "http://localhost:5050";
var repoApiKey = Environment.GetEnvironmentVariable("REPO_API_KEY") ?? string.Empty;
builder.Services.AddSingleton<RepositoryAccessTokenProvider>();
builder.Services.AddTransient<RepositoryBearerTokenHandler>();
builder.Services.AddHttpClient("GeekRepositoryToken");
var repositoryClientBuilder = builder.Services.AddHttpClient("GeekRepository", client =>
    client.BaseAddress = new Uri(repoUrl.TrimEnd('/') + "/"));
if (!string.IsNullOrWhiteSpace(repoApiKey) && builder.Environment.IsDevelopment())
{
    repositoryClientBuilder.ConfigureHttpClient(client =>
        client.DefaultRequestHeaders.Add("X-Repo-Key", repoApiKey));
}
else
{
    repositoryClientBuilder.AddHttpMessageHandler<RepositoryBearerTokenHandler>();
}

var app = builder.Build();

app.Logger.LogInformation("CORS origins: {Origins}", string.Join(", ", corsOrigins));
app.Logger.LogInformation("GeekSeoBackend data path: REPO_URL={RepoUrl}", repoUrl);

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SeoFeatureGateMiddleware>();
app.UseMiddleware<SeoUsageGateMiddleware>();
app.MapControllers();
app.MapHub<SeoContentScoringHub>("/hubs/seo-scoring");

var port = Environment.GetEnvironmentVariable("PORT") ?? "5051";
app.Run($"http://0.0.0.0:{port}");

public partial class Program;
