using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/content-guard")]
public sealed class ContentGuardController(
    ContentGuardService guard,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet("{projectId:guid}/policy")]
    public async Task<IActionResult> GetPolicy(Guid projectId, CancellationToken ct)
    {
        try
        {
            var policy = await guard.GetPolicyAsync(user.RequireUserId(), projectId, ct);
            return policy is null ? NotFound() : Ok(policy);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{projectId:guid}/policy")]
    public async Task<IActionResult> UpsertPolicy(
        Guid projectId,
        [FromBody] UpsertContentGuardPolicyRequest request,
        CancellationToken ct)
    {
        try
        {
            var policy = await guard.UpsertPolicyAsync(user.RequireUserId(), projectId, request, ct);
            return Ok(policy);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{projectId:guid}/runs")]
    public async Task<IActionResult> ListRuns(Guid projectId, CancellationToken ct)
    {
        try
        {
            var runs = await guard.ListRunsAsync(user.RequireUserId(), projectId, ct);
            return Ok(runs);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{projectId:guid}/scan")]
    public async Task<IActionResult> Scan(Guid projectId, CancellationToken ct)
    {
        try
        {
            var policy = await guard.GetPolicyAsync(user.RequireUserId(), projectId, ct);
            await guard.ScanProjectAsync(user.RequireUserId(), projectId, policy?.AutoPatch ?? false, ct);
            return Accepted();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("runs/{runId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid runId, CancellationToken ct)
    {
        try
        {
            var run = await guard.ApproveRunAsync(user.RequireUserId(), runId, ct);
            return Ok(run);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("runs/{runId:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid runId, CancellationToken ct)
    {
        try
        {
            var run = await guard.RollbackRunAsync(user.RequireUserId(), runId, ct);
            return Ok(run);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
