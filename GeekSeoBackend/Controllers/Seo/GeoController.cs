using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/geo")]
public sealed class GeoController(
    GeoVisibilityService geo,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet("platforms")]
    public IActionResult Platforms() => Ok(geo.GetPlatformStatus());

    [HttpPost("probe")]
    public async Task<IActionResult> Probe([FromBody] GeoProbeRequest request, CancellationToken ct)
    {
        try
        {
            var result = await geo.ProbeGoogleAioAsync(user.RequireUserId(), request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
