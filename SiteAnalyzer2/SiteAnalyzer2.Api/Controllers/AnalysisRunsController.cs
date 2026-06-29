using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Services.Integrations;

namespace SiteAnalyzer2.Api.Controllers;

/// <summary>Content Writer contract — keyword analysis list + SERP export.</summary>
[ApiController]
[Route("analysis-runs")]
public sealed class AnalysisRunsController(
    ContentWriterExportService exportService,
    OperatorResearchService operatorResearch) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AnalysisRunSummaryDto>>> List(
        [FromQuery] Guid projectId,
        CancellationToken ct)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = "projectId is required." });

        var runs = await exportService.ListByProjectAsync(projectId, ct);
        return Ok(runs);
    }

    [HttpGet("{runId:guid}/content-writer-export")]
    public async Task<ActionResult<ContentWriterSerpExportDto>> GetContentWriterExport(
        Guid runId,
        CancellationToken ct)
    {
        var export = await exportService.GetExportAsync(runId, ct);
        return export is null ? NotFound() : Ok(export);
    }

    [HttpGet("{runId:guid}/research-focus")]
    public async Task<ActionResult<RunResearchFocusDto>> GetResearchFocus(Guid runId, CancellationToken ct)
    {
        var focus = await operatorResearch.GetResearchFocusAsync(runId, ct);
        return focus is null ? NotFound() : Ok(focus);
    }
}

[ApiController]
[Route("projects/{projectId:guid}/analysis-runs")]
public sealed class ProjectAnalysisRunsController(ContentWriterExportService exportService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AnalysisRunSummaryDto>>> List(
        Guid projectId,
        CancellationToken ct)
    {
        var runs = await exportService.ListByProjectAsync(projectId, ct);
        return Ok(runs);
    }
}
