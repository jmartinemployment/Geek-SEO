using ContentWriter.Application.DTOs;
using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Entities;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ContentWriter.Api.Controllers;

[ApiController]
[Route("api/image-generator/projects/{projectId:guid}")]
public class ImageGeneratorController : ControllerBase
{
    private readonly IProjectRepository _projects;
    private readonly IContentFigureRepository _figures;
    private readonly IFigureDraftGenerationService _draftGeneration;
    private readonly SiteImageStorageOptions _storageOptions;
    private readonly ILogger<ImageGeneratorController> _logger;

    public ImageGeneratorController(
        IProjectRepository projects,
        IContentFigureRepository figures,
        IFigureDraftGenerationService draftGeneration,
        IOptions<SiteImageStorageOptions> storageOptions,
        ILogger<ImageGeneratorController> logger)
    {
        _projects = projects;
        _figures = figures;
        _draftGeneration = draftGeneration;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    [HttpGet("sections")]
    public async Task<ActionResult<ImageGeneratorSectionsResponse>> ListSections(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return NotFound();
        }

        var rows = await _figures.ListByProjectAsync(projectId, cancellationToken);
        var outputRoot = _storageOptions.ResolveLocalOutputRoot();
        var sections = rows
            .OrderBy(f => f.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.SectionOrder)
            .Select(f => ToSectionDto(f, outputRoot))
            .ToList();

        return Ok(new ImageGeneratorSectionsResponse(projectId, sections));
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
            var figure = await _draftGeneration.GenerateDraftAsync(
                projectId,
                source,
                headingSlug,
                cancellationToken);
            return Ok(ToDto(figure));
        }
        catch (ContentGenerationException ex)
        {
            _logger.LogWarning(ex, "Image generator draft failed for project {ProjectId}", projectId);
            return Problem(ex.Message, statusCode: 400, title: "Generate failed");
        }
    }

    private ImageGeneratorSectionDto ToSectionDto(ContentFigure figure, string outputRoot)
    {
        string? relativePath = null;
        var existsOnDisk = false;

        if (!string.IsNullOrWhiteSpace(figure.GeekApiSlug))
        {
            relativePath = FigurePublicPathBuilder.BuildRelativePath(
                figure.GeekApiSlug,
                figure.HeadingSlug);
            var absolute = Path.Combine(
                outputRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            existsOnDisk = System.IO.File.Exists(absolute);
        }

        return new ImageGeneratorSectionDto(
            figure.SourceType,
            figure.HeadingSlug,
            figure.Heading,
            figure.BriefText,
            figure.GeekApiSlug,
            relativePath,
            existsOnDisk,
            figure.ImageUrl,
            figure.Status.ToString());
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
}

public sealed record ImageGeneratorSectionDto(
    string SourceType,
    string HeadingSlug,
    string Heading,
    string BriefText,
    string? GeekApiSlug,
    string? RelativePath,
    bool ExistsOnDisk,
    string? ImageUrl,
    string Status);

public sealed record ImageGeneratorSectionsResponse(
    Guid ProjectId,
    IReadOnlyList<ImageGeneratorSectionDto> Sections);
