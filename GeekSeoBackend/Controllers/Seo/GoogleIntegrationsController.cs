using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/integrations/google")]
public sealed class GoogleIntegrationsController(IGoogleOAuthService google, ICurrentUserContext user) : ControllerBase
{
    [HttpGet("connect-url")]
    public async Task<IActionResult> GetConnectUrl(
        [FromQuery] Guid projectId,
        [FromQuery] string? propertyId,
        [FromQuery] string? siteUrl,
        CancellationToken ct)
    {
        try
        {
            var response = await google.GetConnectUrlAsync(user.RequireUserId(), projectId, propertyId, siteUrl, ct);
            return Ok(response);
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        var appBase = ResolveAppBaseUrl();
        if (!string.IsNullOrWhiteSpace(error))
        {
            return Redirect($"{appBase}/app/projects?google=error&message={Uri.EscapeDataString(error)}");
        }

        try
        {
            var response = await google.HandleCallbackAsync(code, state, ct);
            return Redirect($"{appBase}/app/projects/{response.ProjectId}?google=connected");
        }
        catch (GoogleIntegrationException ex)
        {
            return Redirect($"{appBase}/app/projects?google=error&message={Uri.EscapeDataString(ex.Message)}");
        }
        catch (Exception ex)
        {
            var msg = ex.Message.Length > 200 ? ex.Message[..200] : ex.Message;
            return Redirect($"{appBase}/app/projects?google=error&message={Uri.EscapeDataString($"Unexpected error: {msg}")}");
        }
    }

    private static string ResolveAppBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("GEEKSEO_APP_URL");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim().TrimEnd('/');

        var cors = Environment.GetEnvironmentVariable("CORS_ORIGINS");
        var first = cors?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(first))
            return first.TrimEnd('/');

        return "http://localhost:3000";
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] Guid projectId, CancellationToken ct)
    {
        try
        {
            var response = await google.GetStatusAsync(user.RequireUserId(), projectId, ct);
            return Ok(response);
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect([FromQuery] Guid projectId, CancellationToken ct)
    {
        try
        {
            await google.DisconnectAsync(user.RequireUserId(), projectId, ct);
            return NoContent();
        }
        catch (GoogleIntegrationException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
