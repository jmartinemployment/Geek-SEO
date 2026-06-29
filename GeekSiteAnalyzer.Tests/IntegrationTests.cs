using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Services;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;

namespace SiteAnalyzer2.Tests;

public class IntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
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
        Environment.SetEnvironmentVariable("SERP_EXECUTION", "manual");
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
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null) await _factory.DisposeAsync();
        if (_postgres != null) await _postgres.DisposeAsync();
        Environment.SetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER", null);
        Environment.SetEnvironmentVariable("CORS_ORIGINS", null);
        Environment.SetEnvironmentVariable("SERP_EXECUTION", null);
    }

    [Fact]
    public async Task Pipeline_SerpAndFilterStages_PassGates()
    {
        if (!DockerAvailable)
        {
            return; // skipped when Docker unavailable
        }
        var projectResp = await _client!.PostAsJsonAsync("/projects", new { name = "Test Project" });
        projectResp.EnsureSuccessStatusCode();
        var project = await projectResp.Content.ReadFromJsonAsync<ProjectResponse>();

        var runResp = await _client.PostAsJsonAsync($"/projects/{project!.Id}/runs", new
        {
            keyword = SerpCanonicalFixture.Keyword,
            targetSiteUrl = "https://example.com",
            serpProviderKey = "manual-html"
        });
        runResp.EnsureSuccessStatusCode();
        var run = await runResp.Content.ReadFromJsonAsync<RunResponse>();

        var html = await File.ReadAllTextAsync(TestFixtures.CanonicalSerpHtmlPath());
        using var importContent = new StringContent(html, System.Text.Encoding.UTF8, "text/html");
        var importResp = await _client.PostAsync($"/runs/{run!.Id}/serp/import-html", importContent);
        importResp.EnsureSuccessStatusCode();

        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var serpCount = await db.SerpItems.CountAsync(
            r => r.RunId == run.Id && r.Type == SerpItemTypes.Organic);
        Assert.True(serpCount >= 1);

        var serpGate = await db.RunGates.FirstAsync(g => g.RunId == run!.Id && g.Stage == PipelineStage.Serp);
        Assert.True(serpGate.Passed);
        Assert.Equal(RunStatus.SerpReady, await db.AnalysisRuns.Where(r => r.Id == run!.Id).Select(r => r.Status).FirstAsync());

        var advance = await _client.PostAsync($"/runs/{run!.Id}/stages/Filter/advance", null);
        advance.EnsureSuccessStatusCode();

        var filterGate = await db.RunGates.FirstAsync(g => g.RunId == run.Id && g.Stage == PipelineStage.Filter);
        Assert.True(filterGate.Passed);

        var filteredOrganic = await db.SerpItems.CountAsync(
            c => c.RunId == run.Id && c.Type == SerpItemTypes.Organic && c.FilterStatus != null);
        Assert.True(filteredOrganic >= 1);
    }

    [Fact]
    public async Task BusinessProfile_BeforeExtract_ReturnsNotAvailableReason()
    {
        if (!DockerAvailable) return;

        var projectResp = await _client!.PostAsJsonAsync("/projects", new { name = "Profile Test" });
        projectResp.EnsureSuccessStatusCode();
        var project = await projectResp.Content.ReadFromJsonAsync<ProjectResponse>();

        var runResp = await _client.PostAsJsonAsync($"/projects/{project!.Id}/runs", new
        {
            keyword = "best crm software",
            targetSiteUrl = "https://example.com",
            serpProviderKey = "fixture"
        });
        runResp.EnsureSuccessStatusCode();
        var run = await runResp.Content.ReadFromJsonAsync<RunResponse>();

        var response = await _client.GetAsync($"/runs/{run!.Id}/business-profile");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<BusinessProfileNotAvailableResponse>();
        Assert.NotNull(body);
        Assert.Equal("business_profile_not_available", body!.Error);
        Assert.Equal("extract_stage_not_completed", body.Reason);
    }

    [Fact]
    public async Task BusinessProfile_AfterGeneration_ReturnsContractShape()
    {
        if (!DockerAvailable) return;

        var projectResp = await _client!.PostAsJsonAsync("/projects", new { name = "Profile Contract" });
        projectResp.EnsureSuccessStatusCode();
        var project = await projectResp.Content.ReadFromJsonAsync<ProjectResponse>();

        var runResp = await _client.PostAsJsonAsync($"/projects/{project!.Id}/runs", new
        {
            keyword = "best crm software",
            targetSiteUrl = "https://example.com",
            serpProviderKey = "fixture"
        });
        runResp.EnsureSuccessStatusCode();
        var run = await runResp.Content.ReadFromJsonAsync<RunResponse>();

        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.RunGates.Add(new RunGate
        {
            Id = Guid.NewGuid(),
            ProjectId = project!.Id,
            RunId = run!.Id,
            Stage = PipelineStage.Extract,
            Passed = true,
            ValidationMessage = "seeded",
            RowCountsJson = "{}",
            CheckedAt = DateTime.UtcNow
        });
        db.TargetSiteBusinessProfiles.Add(new TargetSiteBusinessProfile
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            RunId = run.Id,
            TargetSiteUrl = "https://example.com",
            BusinessType = "ProfessionalService",
            PrimaryServicesJson = "[\"Consulting\"]",
            Description = "Example business",
            GeneratedSchemaJson = "{\"@context\":\"https://schema.org\",\"@type\":\"Organization\",\"name\":\"Example\"}",
            HasExistingSchema = false,
            GeneratedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/runs/{run.Id}/business-profile");
        response.EnsureSuccessStatusCode();

        var profile = await response.Content.ReadFromJsonAsync<BusinessProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("ProfessionalService", profile!.BusinessType);
        Assert.Single(profile.PrimaryServices);
        Assert.False(profile.HasExistingSchema);
        Assert.Null(profile.ExistingSchemaMatches);
        Assert.NotEqual(default, profile.LastGeneratedAt);
    }

    [Fact]
    public async Task BusinessProfile_PutAfterExtract_PersistsManualProfile()
    {
        if (!DockerAvailable) return;

        var projectResp = await _client!.PostAsJsonAsync("/projects", new { name = "Manual Profile" });
        projectResp.EnsureSuccessStatusCode();
        var project = await projectResp.Content.ReadFromJsonAsync<ProjectResponse>();

        var runResp = await _client.PostAsJsonAsync($"/projects/{project!.Id}/runs", new
        {
            keyword = "best crm software",
            targetSiteUrl = "https://example.com",
            serpProviderKey = "fixture"
        });
        runResp.EnsureSuccessStatusCode();
        var run = await runResp.Content.ReadFromJsonAsync<RunResponse>();

        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.RunGates.Add(new RunGate
        {
            Id = Guid.NewGuid(),
            ProjectId = project!.Id,
            RunId = run!.Id,
            Stage = PipelineStage.Extract,
            Passed = true,
            ValidationMessage = "seeded",
            RowCountsJson = "{}",
            CheckedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var putResponse = await _client.PutAsJsonAsync($"/runs/{run.Id}/business-profile", new
        {
            businessType = "ProfessionalService",
            primaryServices = new[] { "Consulting", "Managed IT" },
            serviceArea = "United States",
            description = "Human-authored business summary.",
            generatedSchemaJson = new
            {
                context = "https://schema.org",
                type = "Organization",
                name = "Example"
            },
            hasExistingSchema = false
        });
        putResponse.EnsureSuccessStatusCode();

        var profile = await putResponse.Content.ReadFromJsonAsync<BusinessProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Human-authored business summary.", profile!.Description);
        Assert.Equal(2, profile.PrimaryServices.Count);
    }

    [Fact]
    public async Task CompetitorCrawlProgressStream_FlushesHeadersAndCorsImmediately()
    {
        if (!DockerAvailable)
        {
            return;
        }

        Assert.NotNull(_client);

        var runId = Guid.NewGuid();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/runs/{runId}/competitor-crawl/progress-stream");
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:3000");
        request.Headers.TryAddWithoutValidation("Accept", "text/event-stream");

        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").Single());

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var firstLine = await reader.ReadLineAsync(readCts.Token);

        Assert.Equal(": connected", firstLine);
    }

    private record ProjectResponse(Guid Id, string Name);
    private record RunResponse(Guid Id, string Status, string? CurrentStage);
    private record BusinessProfileNotAvailableResponse(string Error, string Reason);
    private record BusinessProfileResponse(
        string BusinessType,
        List<string> PrimaryServices,
        string? ServiceArea,
        string Description,
        JsonElement GeneratedSchemaJson,
        bool HasExistingSchema,
        bool? ExistingSchemaMatches,
        DateTime LastGeneratedAt,
        Guid? ReusedFromRunId);
}
