using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/url-analyzer")]
public sealed class UrlAnalyzerController(
    ISerpResearchPackService research,
    ICurrentUserContext user) : ControllerBase
{
    [HttpPost("research")]
    public async Task<IActionResult> Research(
        [FromBody] UrlAnalyzerResearchRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
            return BadRequest(new { error = "keyword is required" });

        try
        {
            var result = await research.BuildAsync(user.RequireUserId(), request, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            var message = result.Error ?? "SERP research failed";
            var status = message.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
                || message.Contains("SERP", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status502BadGateway
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, new { error = message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required" });
        }
    }
}
