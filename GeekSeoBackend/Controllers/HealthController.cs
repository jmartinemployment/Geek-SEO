using System.Text.Json;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Providers.Seo;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(
    IHttpClientFactory httpClientFactory,
    SeoProviderConfiguration providerConfig,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Liveness + data-gateway check. Verifies GeekAPI (which verifies GeekRepository) — never calls the repo or DB directly.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var gateway = "ok";
        try
        {
            var http = httpClientFactory.CreateClient(GeekDataGateway.HttpClientName);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await http.GetAsync("health", cts.Token);
            if (!response.IsSuccessStatusCode)
                gateway = "error";
            else
            {
                var body = await response.Content.ReadFromJsonAsync<GeekApiHealthPayload>(
                    cancellationToken: cts.Token,
                    options: JsonOptions);
                if (body is not null
                    && !string.Equals(body.Status, "ok", StringComparison.OrdinalIgnoreCase))
                    gateway = "error";
                else if (body is not null
                    && !string.Equals(body.Database, "ok", StringComparison.OrdinalIgnoreCase))
                    gateway = "degraded";
            }
        }
        catch
        {
            gateway = "error";
        }

        var status = gateway switch
        {
            "ok" => "ok",
            "degraded" => "degraded",
            _ => "degraded",
        };

        return Ok(new
        {
            status,
            timestamp = DateTime.UtcNow,
            service = "GeekSeoBackend",
            gateway,
        });
    }

    /// <summary>Which SEO provider implementations are configured (no secret values).</summary>
    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        return Ok(new
        {
            serpProvider = providerConfig.SerpProvider,
            serpProviderFallback = providerConfig.SerpProviderFallback,
            keywordProvider = providerConfig.KeywordProvider,
            rankSnapshotProvider = providerConfig.RankSnapshotProvider,
            vendorPersistence = "database-first",
            vendorRetentionDays = new
            {
                serp = providerConfig.SerpRetentionDays,
                keywords = providerConfig.KeywordRetentionDays,
            },
            credentials = new
            {
                dataforseo = providerConfig.DataForSeoCredentialsConfigured,
                serpapi = providerConfig.SerpApiKeyConfigured,
                serperDev = providerConfig.SerperDevApiKeyConfigured,
                anthropic = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")),
                anthropicModel = ClaudeProvider.ResolveModel(null),
            },
        });
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record GeekApiHealthPayload(string Status, string? Database);
}
