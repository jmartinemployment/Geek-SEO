using GeekSeoBackend.Models;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicScanController(IPublicSiteScanService scanService) : ControllerBase
{
    [HttpGet("scan")]
    public async Task<IActionResult> Scan([FromQuery] string url, CancellationToken ct)
    {
        var (ok, result, error) = await scanService.ScanAsync(url, ct);
        if (!ok || result is null)
            return BadRequest(new PublicScanErrorResponse(error ?? "Invalid URL."));

        return Ok(result);
    }
}
