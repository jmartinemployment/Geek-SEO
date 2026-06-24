using GeekSeoBackend.Auth;
using GeekSeo.Application.Interfaces.Seo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/analysis-runs")]
[Authorize]
public sealed class AnalysisRunController(
    IAnalysisRunRepository analysisRuns,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid projectId, CancellationToken ct)
    {
        var result = await analysisRuns.ListByProjectAsync(projectId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}/content-writer-export")]
    public async Task<IActionResult> GetContentWriterExport(Guid id, CancellationToken ct)
    {
        var result = await analysisRuns.GetContentWriterExportAsync(id, ct);
        if (!result.IsSuccess)
            return result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound()
                : BadRequest(result.Error);
        return Ok(result.Value);
    }
}
