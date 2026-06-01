using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/topical-map")]
public sealed class TopicalMapController(
    TopicalMapService topicalMap,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        try
        {
            var cached = await topicalMap.GetCachedAsync(user.RequireUserId(), projectId, ct);
            return cached is null ? NotFound() : Ok(cached);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{projectId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid projectId, [FromQuery] bool force = false, CancellationToken ct = default)
    {
        try
        {
            var result = await topicalMap.GenerateAsync(user.RequireUserId(), projectId, force, ct);
            return Ok(result);
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
