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
    public async Task<IActionResult> Generate(
        Guid projectId,
        [FromQuery] string? seedKeyword = null,
        [FromQuery] string? location = null,
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(seedKeyword))
            {
                var result = await topicalMap.GenerateSeedModeAsync(user.RequireUserId(), projectId, seedKeyword, location, ct);
                return Ok(result);
            }

            var gscResult = await topicalMap.GenerateAsync(user.RequireUserId(), projectId, force, ct);
            return Ok(gscResult);
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

    [HttpGet("{projectId:guid}/linking-blueprint")]
    public async Task<IActionResult> GetLinkingBlueprint(Guid projectId, CancellationToken ct)
    {
        try
        {
            var cached = await topicalMap.GetCachedAsync(user.RequireUserId(), projectId, ct);
            if (cached?.LinkingBlueprint is null)
                return NotFound();
            return Ok(cached.LinkingBlueprint);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{projectId:guid}/entity-gaps")]
    public async Task<IActionResult> GetEntityGaps(Guid projectId, CancellationToken ct)
    {
        try
        {
            var cached = await topicalMap.GetCachedAsync(user.RequireUserId(), projectId, ct);
            if (cached is null)
                return NotFound();

            var gapAnalysis = cached.Topics
                .OrderBy(t => t.EntityCoverage)
                .Select(t => new
                {
                    t.Name,
                    t.MainKeyword,
                    t.Tier,
                    t.EntityCoverage,
                    t.EntityGaps,
                    GapCount = t.EntityGaps.Count,
                })
                .ToList();

            return Ok(gapAnalysis);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
