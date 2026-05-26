using GeekApplication.Interfaces.Seo;
using GeekApplication.Models.Seo;
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

        var result = await serp.AnalyzeAsync(user.RequireUserId(), new DeepSerpRequest
        {
            Keyword = keyword,
            Location = location,
            LanguageCode = languageCode,
        }, ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
