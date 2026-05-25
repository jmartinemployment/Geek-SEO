using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var repository = "ok";
        try
        {
            var http = httpClientFactory.CreateClient("GeekRepository");
            using var response = await http.GetAsync(
                "repo/seo/projects?userId=00000000-0000-0000-0000-000000000000",
                cancellationToken);
            if (response.StatusCode is not System.Net.HttpStatusCode.OK
                and not System.Net.HttpStatusCode.NotFound)
            {
                repository = "error";
            }
        }
        catch
        {
            repository = "error";
        }

        return Ok(new
        {
            status = repository == "ok" ? "ok" : "degraded",
            timestamp = DateTime.UtcNow,
            service = "GeekSeoBackend",
            repository,
        });
    }
}
