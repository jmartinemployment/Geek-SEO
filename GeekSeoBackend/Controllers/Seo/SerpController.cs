using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/serp")]
public sealed class SerpController(ISerpAnalysisService serp, ICurrentUserContext user) : ControllerBase
{
    [HttpGet("deep")]
    public async Task<IActionResult> Deep(
        [FromQuery] string keyword,
        [FromQuery] string location = "United States",
        [FromQuery] string languageCode = "en",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest("keyword is required");

        try
        {
            var result = await serp.AnalyzeAsync(user.RequireUserId(), new DeepSerpRequest
            {
                Keyword = keyword.Trim(),
                Location = location,
                LanguageCode = languageCode,
            }, ct);

            if (result.IsSuccess)
                return Ok(result.Value);

            var message = result.Error ?? "SERP analysis failed";
            var status = message.Contains("DATAFORSEO", StringComparison.OrdinalIgnoreCase)
                || message.Contains("DataForSEO", StringComparison.OrdinalIgnoreCase)
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
