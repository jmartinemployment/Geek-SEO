using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/plagiarism")]
public sealed class PlagiarismController(IPlagiarismService plagiarism, ICurrentUserContext user) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status() => Ok(plagiarism.GetStatus());

    [HttpPost("check")]
    public async Task<IActionResult> Check([FromBody] PlagiarismCheckRequest request, CancellationToken ct)
    {
        var result = await plagiarism.CheckDocumentAsync(user.RequireUserId(), request, ct);
        if (!result.IsSuccess)
        {
            return result.Status == GeekSeo.Application.Results.ResultStatus.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpGet("check/{documentId:guid}")]
    public async Task<IActionResult> GetLatest(Guid documentId, CancellationToken ct)
    {
        var result = await plagiarism.GetLatestForDocumentAsync(user.RequireUserId(), documentId, ct);
        if (!result.IsSuccess)
        {
            return result.Status == GeekSeo.Application.Results.ResultStatus.NotFound
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        }

        if (result.Value is null)
            return NoContent();

        return Ok(result.Value);
    }
}
