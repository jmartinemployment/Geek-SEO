using ContentWriter.Application.Services;
using ContentWriter.Application.Services.Export;
using Microsoft.AspNetCore.Mvc;

namespace ContentWriter.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/export")]
public class ExportController : ControllerBase
{
    private readonly IContentMarkdownExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IContentMarkdownExportService exportService, ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    [HttpPost("markdown")]
    public async Task<IActionResult> ExportMarkdown(
        Guid projectId,
        [FromBody] ExportMarkdownRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _exportService.ExportAsync(
                projectId,
                request?.Department,
                cancellationToken);

            return Ok(new ExportMarkdownResponse(
                result.Department,
                result.Files.Select(f => new ExportedMarkdownFileResponse(f.ContentType, f.FilePath)).ToList()));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Markdown export failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Export failed");
        }
    }
}

public sealed record ExportMarkdownRequest(string? Department);

public sealed record ExportMarkdownResponse(string Department, IReadOnlyList<ExportedMarkdownFileResponse> Files);

public sealed record ExportedMarkdownFileResponse(string ContentType, string FilePath);
