using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/google")]
public sealed class GoogleSignalsController(IGoogleOAuthService google, ICurrentUserContext user) : ControllerBase
{
    [HttpGet("signals/{projectId:guid}")]
    public async Task<IActionResult> Signals(Guid projectId, CancellationToken ct)
    {
        try
        {
            var status = await google.GetStatusAsync(user.RequireUserId(), projectId, ct);
            return Ok(new
            {
                projectId,
                googleConnected = status.Connected,
                gscConnected = status.GscConnected,
                ga4Connected = status.Ga4Connected,
                status.SiteUrl,
                status.PropertyId,
                status.ConnectedAt,
            });
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
