using Microsoft.AspNetCore.Mvc;
using SiteAnalyzer2.Serp;
using SiteAnalyzer2.Services.Integrations;

namespace SiteAnalyzer2.Api.Controllers;

/// <summary>Content Writer contract — keyword analysis list + SERP export.</summary>
[ApiController]
[Route("analysis-runs")]
public sealed class AnalysisRunsController(
    ContentWriterExportService exportService,
    OperatorResearchService operatorResearch,
    ManualLaneImportService manualLaneImport) : ControllerBase
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

    [HttpPost("{runId:guid}/serp/import-html")]
    public async Task<IActionResult> ImportSerpHtml(
        Guid runId,
        [FromQuery] string? lane,
        [FromQuery] string topic,
        CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var html = await reader.ReadToEndAsync(ct);

        try
        {
            var result = await manualLaneImport.ImportLaneAsync(runId, html, lane, topic, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    [HttpPost("{runId:guid}/serp/import-paa-batch")]
    public async Task<IActionResult> ImportPaaBatch(
        Guid runId,
        [FromQuery] string topic,
        [FromBody] PaaBatchImportRequest request,
        CancellationToken ct)
    {
        try
        {
            var files = (request.Files ?? [])
                .Select(f => new PaaLaneImportFile(f.FileName, f.Content))
                .ToList();
            var result = await manualLaneImport.ImportPaaBatchAsync(runId, files, topic, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    [HttpPatch("{runId:guid}/topic-slug")]
    public async Task<IActionResult> UpdateTopicSlug(
        Guid runId,
        [FromBody] UpdateTopicSlugRequest request,
        CancellationToken ct)
    {
        try
        {
            var slug = await manualLaneImport.UpdateTopicSlugAsync(runId, request.TopicSlug, ct);
            return Ok(new { topicSlug = slug });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }
}

public sealed record UpdateTopicSlugRequest
{
    public required string TopicSlug { get; init; }
}

public sealed record PaaBatchImportRequest
{
    public List<PaaBatchImportFile> Files { get; init; } = [];
}

public sealed record PaaBatchImportFile
{
    public string? FileName { get; init; }
    public required string Content { get; init; }
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
