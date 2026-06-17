using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Infrastructure;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/writing")]
public sealed class WritingController(
    IAIWritingService writing,
    ICurrentUserContext user,
    FullArticleJobChannel fullArticleChannel,
    BulkArticleJobChannel bulkArticleChannel) : ControllerBase
{
    [HttpPost("full-article")]
    public async Task<IActionResult> FullArticle([FromBody] FullArticleRequest request, CancellationToken ct)
    {
        var result = await writing.EnqueueFullArticleAsync(user.RequireUserId(), request, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        fullArticleChannel.Notify();
        return Accepted(result.Value);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Bulk([FromBody] BulkArticleRequest request, CancellationToken ct)
    {
        var result = await writing.EnqueueBulkArticlesAsync(user.RequireUserId(), request, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        bulkArticleChannel.Notify();
        return Accepted(result.Value);
    }

    [HttpPost("outline")]
    public async Task<IActionResult> Outline([FromBody] WritingOutlineRequest request, CancellationToken ct)
    {
        var result = await writing.GenerateOutlineAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("draft")]
    public async Task<IActionResult> Draft([FromBody] WritingDraftRequest request, CancellationToken ct)
    {
        var result = await writing.GenerateDraftAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("humanize")]
    public async Task<IActionResult> Humanize([FromBody] HumanizeRequest request, CancellationToken ct)
    {
        var result = await writing.HumanizeAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("detect")]
    public async Task<IActionResult> Detect([FromBody] DetectAiRequest request, CancellationToken ct)
    {
        var result = await writing.DetectAsync(user.RequireUserId(), request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
