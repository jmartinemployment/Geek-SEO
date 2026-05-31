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

    [HttpGet("queries")]
    public async Task<IActionResult> ListQueries([FromQuery] Guid projectId, CancellationToken ct)
    {
        try
        {
            var queries = await geo.ListQueriesAsync(user.RequireUserId(), projectId, ct);
            return Ok(queries);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("queries")]
    public async Task<IActionResult> CreateQuery([FromBody] CreateGeoTrackingQueryRequest request, CancellationToken ct)
    {
        try
        {
            var created = await geo.CreateQueryAsync(user.RequireUserId(), request, ct);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("queries/{queryId:guid}")]
    public async Task<IActionResult> DeleteQuery(Guid queryId, CancellationToken ct)
    {
        try
        {
            await geo.DeleteQueryAsync(user.RequireUserId(), queryId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("queries/{queryId:guid}/trends")]
    public async Task<IActionResult> Trends(Guid queryId, CancellationToken ct)
    {
        try
        {
            var trends = await geo.GetTrendsAsync(user.RequireUserId(), queryId, ct);
            return Ok(trends);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

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
