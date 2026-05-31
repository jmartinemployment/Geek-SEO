using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/cannibalization")]
public sealed class CannibalizationController(
    CannibalizationService cannibalization,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(
        Guid projectId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken ct)
    {
        try
        {
            var report = await cannibalization.AnalyzeAsync(
                user.RequireUserId(), projectId, startDate, endDate, ct);
            return Ok(report);
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
