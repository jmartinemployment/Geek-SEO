using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/url-research")]
public sealed class UrlResearchController(
    IUrlResearchService research,
    IProjectRepository projects,
    UrlResearchJobChannel channel,
    ICurrentUserContext user) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        [FromBody] UrlResearchAnalyzeRequest request,
        CancellationToken ct)
    {
        if (request.ProjectId == Guid.Empty)
            return BadRequest(new { error = "projectId is required" });
        if (string.IsNullOrWhiteSpace(request.PageUrl))
            return BadRequest(new { error = "pageUrl is required" });

        var userId = user.RequireUserId();
        var project = await projects.GetByIdAsync(request.ProjectId, userId, ct);
        if (!project.IsSuccess || project.Value is null)
            return project.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { error = "Project not found" })
                : StatusCode(StatusCodes.Status403Forbidden, new { error = "Access denied" });

        var pageUrl = UrlPageKeywordResolver.NormalizeUrl(request.PageUrl);
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out _))
            return BadRequest(new { error = "pageUrl must be a valid absolute URL" });

        if (!RegistrableDomainMatcher.SameRegistrableDomain(pageUrl, project.Value.Url))
        {
            return BadRequest(new
            {
                error = "pageUrl must be on the same domain as the project (subdomains allowed).",
            });
        }

        var queued = await research.CreateQueuedAsync(
            userId,
            new CreateUrlResearchQueuedRequest
            {
                ProjectId = request.ProjectId,
                SourceUrl = pageUrl,
            },
            ct);

        if (!queued.IsSuccess || queued.Value is null)
        {
            var status = queued.Error?.Contains("Access denied", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = queued.Error ?? "Could not enqueue page research" });
        }

        channel.Notify();
        return Accepted(new
        {
            urlResearchId = queued.Value.Id,
            status = "queued",
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
