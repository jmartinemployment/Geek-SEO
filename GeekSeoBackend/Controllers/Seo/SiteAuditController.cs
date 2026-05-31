using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/audit")]
public sealed class SiteAuditController(ISiteAuditService audits, ICurrentUserContext user) : ControllerBase
{
    [HttpPost("site")]
    public async Task<IActionResult> Start([FromBody] CreateSiteAuditRequest request, CancellationToken ct)
    {
        var result = await audits.StartAsync(user.RequireUserId(), request.ProjectId, ct);
        if (!result.IsSuccess)
        {
            return result.Status == GeekSeo.Application.Results.ResultStatus.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Accepted(result.Value);
    }

    [HttpGet("site/{auditId:guid}")]
    public async Task<IActionResult> Get(Guid auditId, CancellationToken ct)
    {
        var result = await audits.GetAsync(user.RequireUserId(), auditId, ct);
        if (!result.IsSuccess)
        {
            return result.Status == GeekSeo.Application.Results.ResultStatus.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("site")]
    public async Task<IActionResult> List([FromQuery] Guid projectId, CancellationToken ct)
    {
        var result = await audits.ListByProjectAsync(user.RequireUserId(), projectId, ct);
        if (!result.IsSuccess)
        {
            return result.Status == GeekSeo.Application.Results.ResultStatus.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
