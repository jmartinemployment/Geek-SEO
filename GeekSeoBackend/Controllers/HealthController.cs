using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Liveness for Railway/deploy probes — must not call OAuth or GeekRepository (those can block for minutes when the issuer is down).
    /// </summary>
    [HttpGet]
    public IActionResult Get() =>
        Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow,
            service = "GeekSeoBackend",
        });
}
