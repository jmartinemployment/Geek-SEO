using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/url-research")]
public sealed class UrlResearchController(
    IUrlResearchService research,
    ICurrentUserContext user) : ControllerBase
{
    [HttpPost("analyze")]
    [Obsolete("Retired — content writing pipeline removed.")]
    public IActionResult Analyze([FromBody] UrlResearchAnalyzeRequest request)
    {
        _ = request;
        return StatusCode(StatusCodes.Status410Gone, new
        {
            error = "URL research analyze is retired. A new content writer will replace this flow.",
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await research.GetFullAsync(user.RequireUserId(), id, ct);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            if (result.Error?.Contains("Access denied", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid projectId,
        CancellationToken ct)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = "projectId is required" });

        var result = await research.ListSummaryByProjectAsync(user.RequireUserId(), projectId, ct);
        if (!result.IsSuccess)
        {
            var status = result.Error?.Contains("Access denied", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = result.Error });
        }

        return Ok(result.Value);
    }
}

public sealed record UrlResearchAnalyzeRequest
{
    public required Guid ProjectId { get; init; }
    public required string PageUrl { get; init; }
}
