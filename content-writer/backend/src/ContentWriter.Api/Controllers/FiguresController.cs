using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ContentWriter.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/figures")]
public class FiguresController : ControllerBase
{
    private readonly IContentFigureRepository _figures;
    private readonly IProjectRepository _projects;
    private readonly IContentFigureAttachService _attachService;
    private readonly ILogger<FiguresController> _logger;

    public FiguresController(
        IContentFigureRepository figures,
        IProjectRepository projects,
        IContentFigureAttachService attachService,
        ILogger<FiguresController> logger)
    {
        _figures = figures;
        _projects = projects;
        _attachService = attachService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ContentFiguresListResponse>> List(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        var rows = await _figures.ListByProjectAsync(projectId, cancellationToken);
        return Ok(new ContentFiguresListResponse(
            projectId,
            rows.Select(ToDto).ToList(),
            BuildSummary(rows),
            InAppGenerationEnabled: false));
    }

    [HttpGet("export")]
    public async Task<ActionResult<ContentFigureManifestResponse>> ExportManifest(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        var rows = await _figures.ListByProjectAsync(projectId, cancellationToken);
        return Ok(new ContentFigureManifestResponse(
            projectId,
            rows.Select(f => new ContentFigureManifestEntry(
                f.SourceType,
                f.HeadingSlug,
                f.Heading,
                f.SectionOrder,
                f.BriefText,
                f.Status,
                f.ImageUrl,
                f.GeekApiSlug)).ToList()));
    }

    [HttpPost("generate")]
    [Obsolete("Use the external SectionFigures CLI.")]
    public ActionResult<FigureGenerateResponse> Generate(
        Guid projectId,
        [FromBody] FigureGenerateRequest request) =>
        Problem(
            "In-app figure generation is disabled. Use the SectionFigures CLI (export-jobs → generate).",
            statusCode: 410,
            title: "Use SectionFigures CLI");

    [HttpPost("{source}/{headingSlug}/generate")]
    [Obsolete("Use the external SectionFigures CLI.")]
    public ActionResult<ContentFigureDto> GenerateOne(
        Guid projectId,
        string source,
        string headingSlug) =>
        Problem(
            "In-app figure generation is disabled. Use the SectionFigures CLI (export-jobs → generate).",
            statusCode: 410,
            title: "Use SectionFigures CLI");

    [HttpPost("{source}/{headingSlug}/attach")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ContentFigureDto>> Attach(
        Guid projectId,
        string source,
        string headingSlug,
        IFormFile file,
        [FromQuery] string? alt,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        if (file is null || file.Length == 0)
        {
            return Problem("Image file is required.", statusCode: 400, title: "Attach failed");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var figure = await _attachService.AttachAvifAsync(
                projectId,
                source,
                headingSlug,
                stream,
                file.FileName,
                alt,
                cancellationToken);
            return Ok(ToDto(figure));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Figure attach failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Figure attach failed");
        }
    }

    [HttpPost("{source}/{headingSlug}/set-url")]
    public async Task<ActionResult<ContentFigureDto>> SetUrl(
        Guid projectId,
        string source,
        string headingSlug,
        [FromBody] FigureSetUrlRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        try
        {
            var figure = await _attachService.AssignImageUrlAsync(
                projectId,
                source,
                headingSlug,
                request.Url,
                request.Alt,
                cancellationToken);
            return Ok(ToDto(figure));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Figure set-url failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Set URL failed");
        }
    }

    [HttpPost("{source}/{headingSlug}/skip")]
    public async Task<ActionResult<ContentFigureDto>> Skip(
        Guid projectId,
        string source,
        string headingSlug,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        try
        {
            var figure = await _attachService.SkipAsync(projectId, source, headingSlug, cancellationToken);
            return Ok(ToDto(figure));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Figure skip failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Figure skip failed");
        }
    }


    private async Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(projectId, cancellationToken);
        return project is not null;
    }

    private static ContentFigureDto ToDto(ContentFigure figure) =>
        new(
            figure.Id,
            figure.SourceType,
            figure.SectionOrder,
            figure.HeadingSlug,
            figure.Heading,
            figure.BriefText,
            figure.Status,
            figure.SkipReason,
            figure.ImageUrl,
            figure.ImageWidth,
            figure.ImageHeight,
            figure.ImageAlt,
            figure.GeekApiSlug,
            figure.GeekPostId,
            figure.ImagePromptContentId,
            figure.UpdatedAtUtc);

    private static ContentFiguresSummary BuildSummary(IReadOnlyList<ContentFigure> rows) =>
        new(
            rows.Count(f => f.Status == FigureStatus.Pending),
            rows.Count(f => f.Status == FigureStatus.Ready),
            rows.Count(f => f.Status == FigureStatus.Skipped),
            rows.Count(f => f.Status == FigureStatus.Published),
            rows.Count(f => string.IsNullOrWhiteSpace(f.GeekApiSlug)));
}
