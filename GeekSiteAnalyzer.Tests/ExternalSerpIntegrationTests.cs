using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace SiteAnalyzer2.Tests;

public class ExternalSerpIntegrationTests : IAsyncLifetime
{
    private const string WorkerApiKey = "integration-test-worker-key";

    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpClient? _workerClient;
    private static readonly bool DockerAvailable = CheckDockerAvailable();

    private static bool CheckDockerAvailable()
    {
        try
        {
            using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            proc?.WaitForExit(5000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task InitializeAsync()
    {
        if (!DockerAvailable) return;

        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", "human");
        Environment.SetEnvironmentVariable("CORS_ORIGINS", "http://localhost:3000");
        Environment.SetEnvironmentVariable("SERP_EXECUTION", "external");
        Environment.SetEnvironmentVariable("SERP_WORKER_API_KEY", WorkerApiKey);

        _postgres = new PostgreSqlBuilder().WithImage("postgres:16").Build();
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
                builder.ConfigureServices(services =>
                {
                    services.AddScoped<IRunProgressNotifier, NoOpRunProgressNotifier>();
                });
            });

        _client = _factory.CreateClient();
        _workerClient = _factory.CreateClient();
        _workerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", WorkerApiKey);
    }

    public async Task DisposeAsync()
    {
        _workerClient?.Dispose();
        _client?.Dispose();
        if (_factory != null) await _factory.DisposeAsync();
        if (_postgres != null) await _postgres.DisposeAsync();
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
        Environment.SetEnvironmentVariable("CORS_ORIGINS", null);
        Environment.SetEnvironmentVariable("SERP_EXECUTION", null);
        Environment.SetEnvironmentVariable("SERP_WORKER_API_KEY", null);
    }

    [Fact]
    public async Task ExternalSerp_ClaimAndWorkerResult_CompletesSerpGate()
    {
        if (!DockerAvailable) return;

        var projectResp = await _client!.PostAsJsonAsync("/projects", new { name = "External Serp" });
        projectResp.EnsureSuccessStatusCode();
        var project = await projectResp.Content.ReadFromJsonAsync<ProjectResponse>();

        var runResp = await _client.PostAsJsonAsync($"/projects/{project!.Id}/runs", new
        {
            keyword = "best crm software",
            targetSiteUrl = "https://example.com",
            serpProviderKey = "google-scraper"
        });
        runResp.EnsureSuccessStatusCode();
        var run = await runResp.Content.ReadFromJsonAsync<RunResponse>();
        Assert.Equal("Running", run!.Status);

        var pendingResp = await _workerClient!.GetAsync("/internal/runs/pending-serp");
        pendingResp.EnsureSuccessStatusCode();
        var pending = await pendingResp.Content.ReadFromJsonAsync<PendingSerpRunsResponse>();
        Assert.Contains(pending!.Runs, r => r.Id == run.Id);

        var claimResp = await _workerClient.PostAsync($"/internal/runs/{run.Id}/stages/serp/claim", null);
        claimResp.EnsureSuccessStatusCode();

        var html = await File.ReadAllTextAsync(TestFixtures.CanonicalSerpHtmlPath());
        var resultResp = await _workerClient.PostAsJsonAsync(
            $"/internal/runs/{run.Id}/stages/serp/worker-result",
            new { success = true, html, failureMessage = (string?)null });
        resultResp.EnsureSuccessStatusCode();

        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var serpCount = await db.SerpItems.CountAsync(r => r.RunId == run.Id && r.Type == "organic");
        Assert.True(serpCount >= 1);

        var serpGate = await db.RunGates.FirstAsync(g => g.RunId == run.Id && g.Stage == PipelineStage.Serp);
        Assert.True(serpGate.Passed);
        Assert.Equal(RunStatus.SerpReady, await db.AnalysisRuns.Where(r => r.Id == run.Id).Select(r => r.Status).FirstAsync());
    }

    [Fact]
    public async Task ExternalSerp_InternalEndpoints_RequireWorkerApiKey()
    {
        if (!DockerAvailable) return;

        var response = await _client!.GetAsync("/internal/runs/pending-serp");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record ProjectResponse(Guid Id, string Name);
    private record RunResponse(Guid Id, string Status, string? CurrentStage);
    private record PendingSerpRunsResponse(IReadOnlyList<PendingSerpRunItem> Runs);
    private record PendingSerpRunItem(Guid Id, Guid ProjectId, string Keyword, string SerpProviderKey);
}
