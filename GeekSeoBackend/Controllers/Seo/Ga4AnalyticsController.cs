using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/analytics/ga4")]
public sealed class Ga4AnalyticsController(IGoogleDataService googleData, ICurrentUserContext user) : ControllerBase
{
    [HttpGet("{projectId:guid}/landing-pages")]
    public async Task<IActionResult> LandingPages(
        Guid projectId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int? limit,
        CancellationToken ct)
    {
        try
        {
            var response = await googleData.GetGa4LandingPagesAsync(
                user.RequireUserId(),
                projectId,
                startDate,
                endDate,
                limit,
                ct);
            return Ok(response);
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
