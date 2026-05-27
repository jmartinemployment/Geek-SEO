using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/rankings")]
public sealed class RankingsController(IGoogleDataService googleData, ICurrentUserContext user) : ControllerBase
{
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(
        Guid projectId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int? rowLimit,
        CancellationToken ct)
    {
        try
        {
            var response = await googleData.GetRankingsAsync(
                user.RequireUserId(),
                projectId,
                startDate,
                endDate,
                rowLimit,
                ct);
            return Ok(response);
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
