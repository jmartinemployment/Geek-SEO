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
    private readonly IFigureMergeService _mergeService;
    private readonly ILogger<FiguresController> _logger;

    public FiguresController(
        IContentFigureRepository figures,
        IProjectRepository projects,
        IFigureMergeService mergeService,
        ILogger<FiguresController> logger)
    {
        _figures = figures;
        _projects = projects;
        _mergeService = mergeService;
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
            BuildSummary(rows)));
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
                f.GeekApiSlug,
                f.NeedsFigureMerge)).ToList()));
    }

    [HttpPost("merge")]
    public async Task<ActionResult<FigureMergeResponse>> Merge(
        Guid projectId,
        [FromBody] FigureMergeRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        try
        {
            FigureMergeService.ValidateSourceType(request.Source);
            var result = await _mergeService.MergeSourceAsync(projectId, request.Source, cancellationToken);
            return Ok(new FigureMergeResponse(
                result.SourceType,
                result.GeekApiSlug,
                result.GeekPostId,
                result.FiguresMerged,
                result.PublicPath));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Figure merge failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Figure merge failed");
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
            figure.NeedsFigureMerge,
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
