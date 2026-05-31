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
    [HttpPost("{projectId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid projectId, CancellationToken ct)
    {
        try
        {
            var result = await topicalMap.GenerateAsync(user.RequireUserId(), projectId, ct);
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
