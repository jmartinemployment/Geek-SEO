using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/content-audit")]
public sealed class ContentAuditController(
    PublishedContentAuditService audit,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        try
        {
            var report = await audit.AnalyzeAsync(user.RequireUserId(), projectId, ct);
            return Ok(report);
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
