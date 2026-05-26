using System.Text.Json;
using GeekSeoBackend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(IHttpClientFactory httpClientFactory) : ControllerBase
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

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record GeekApiHealthPayload(string Status, string? Database);
}
