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
    private readonly IContentFigureAttachService _attachService;
    private readonly IContentFigureImageGenerationService _imageGeneration;
    private readonly ILogger<FiguresController> _logger;

    public FiguresController(
        IContentFigureRepository figures,
        IProjectRepository projects,
        IFigureMergeService mergeService,
        IContentFigureAttachService attachService,
        IContentFigureImageGenerationService imageGeneration,
        ILogger<FiguresController> logger)
    {
        _figures = figures;
        _projects = projects;
        _mergeService = mergeService;
        _attachService = attachService;
        _imageGeneration = imageGeneration;
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

    [HttpPost("generate")]
    public async Task<ActionResult<FigureGenerateResponse>> Generate(
        Guid projectId,
        [FromBody] FigureGenerateRequest request,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        try
        {
            FigureMergeService.ValidateSourceType(request.Source);
            IReadOnlyList<ContentFigure> figures;
            if (string.IsNullOrWhiteSpace(request.HeadingSlug))
            {
                figures = await _imageGeneration.GeneratePendingAsync(
                    projectId,
                    request.Source,
                    cancellationToken);
            }
            else
            {
                figures =
                [
                    await _imageGeneration.GenerateAsync(
                        projectId,
                        request.Source,
                        request.HeadingSlug,
                        cancellationToken),
                ];
            }

            return Ok(new FigureGenerateResponse(
                request.Source,
                figures.Count,
                figures.Select(ToDto).ToList()));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Figure generation failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Figure generation failed");
        }
    }

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
            var figure = await _attachService.AttachWebpAsync(
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

    [HttpPost("{source}/{headingSlug}/generate")]
    public async Task<ActionResult<ContentFigureDto>> GenerateOne(
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
            var figure = await _imageGeneration.GenerateAsync(
                projectId,
                source,
                headingSlug,
                cancellationToken);
            return Ok(ToDto(figure));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Figure generation failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Figure generation failed");
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
