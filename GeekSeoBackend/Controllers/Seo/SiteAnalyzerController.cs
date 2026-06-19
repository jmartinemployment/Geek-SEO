using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/site-analyzer")]
public sealed class SiteAnalyzerController(
    SiteAnalyzerStepService steps,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/state")]
    public async Task<IActionResult> GetState(Guid projectId, CancellationToken ct)
    {
        var result = await steps.GetStateAsync(user.RequireUserId(), projectId, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("projects/{projectId:guid}/site-index/steps/{step:int}/run")]
    public async Task<IActionResult> RunSiteIndexStep(Guid projectId, int step, CancellationToken ct)
    {
        var result = await steps.RunSiteIndexStepAsync(user.RequireUserId(), projectId, step, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, step });
        return Ok(result.Value);
    }

    [HttpPost("projects/{projectId:guid}/packs")]
    public async Task<IActionResult> CreatePack(
        Guid projectId,
        [FromBody] CreateSiteAnalyzerPackRequest request,
        CancellationToken ct)
    {
        var result = await steps.CreatePackAsync(user.RequireUserId(), projectId, request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("packs/{urlResearchId:guid}/steps/{step:int}/run")]
    public async Task<IActionResult> RunPackStep(Guid urlResearchId, int step, CancellationToken ct)
    {
        var result = await steps.RunPackStepAsync(user.RequireUserId(), urlResearchId, step, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, step });
        return Ok(result.Value);
    }
}
