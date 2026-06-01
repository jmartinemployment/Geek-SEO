using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/rank-tracker")]
public sealed class RankTrackingController(
    RankTrackingService rankTracking) : ControllerBase
{
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetKeywords(
        Guid projectId,
        CancellationToken ct)
    {
        try
        {
            var result = await rankTracking.GetTrackedKeywordsAsync(projectId, ct);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{projectId:guid}")]
    public async Task<IActionResult> AddKeyword(
        Guid projectId,
        [FromBody] TrackedKeywordRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await rankTracking.AddTrackedKeywordAsync(projectId, request, ct);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("keyword/{keywordId:guid}")]
    public async Task<IActionResult> DeleteKeyword(
        Guid keywordId,
        CancellationToken ct)
    {
        try
        {
            var result = await rankTracking.DeleteTrackedKeywordAsync(keywordId, ct);
            return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{projectId:guid}/history")]
    public async Task<IActionResult> GetHistory(
        Guid projectId,
        [FromQuery] string keyword,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        try
        {
            var result = await rankTracking.GetRankHistoryAsync(projectId, keyword, days, ct);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
