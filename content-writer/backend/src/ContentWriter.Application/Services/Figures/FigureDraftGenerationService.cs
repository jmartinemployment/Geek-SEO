using ContentWriter.Application.Providers;
using ContentWriter.Application.Services;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;

namespace ContentWriter.Application.Services.Figures;

/// <summary>
/// Single-section OpenAI draft for the /image-generator app. Not gated by InAppGenerationEnabled.
/// </summary>
public interface IFigureDraftGenerationService
{
    Task<ContentFigure> GenerateDraftAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default);
}

public sealed class FigureDraftGenerationService : IFigureDraftGenerationService
{
    private readonly IContentFigureRepository _figures;
    private readonly IContentFigureAttachService _attach;
    private readonly OpenAiFigureImageClient _imageClient;

    public FigureDraftGenerationService(
        IContentFigureRepository figures,
        IContentFigureAttachService attach,
        OpenAiFigureImageClient imageClient)
    {
        _figures = figures;
        _attach = attach;
        _imageClient = imageClient;
    }

    public async Task<ContentFigure> GenerateDraftAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default)
    {
        FigureSourceValidator.ValidateSourceType(sourceType);

        var figure = await _figures.GetByHeadingSlugAsync(projectId, sourceType, headingSlug, cancellationToken);
        if (figure is null)
        {
            throw new ContentGenerationException(
                $"No figure row for source={sourceType}, headingSlug={headingSlug}.");
        }

        if (figure.Status == FigureStatus.Skipped)
        {
            throw new ContentGenerationException($"Figure {headingSlug} is Skipped.");
        }

        if (string.IsNullOrWhiteSpace(figure.GeekApiSlug))
        {
            throw new ContentGenerationException(
                "Publish text to geekatyourspot first, then generate art.");
        }

        if (string.IsNullOrWhiteSpace(figure.BriefText))
        {
            throw new ContentGenerationException(
                $"Figure brief for \"{figure.Heading}\" is empty. Run Step 6 first.");
        }

        var prompt = FigureImagePromptComposer.Compose(figure.BriefText, figure.Heading);
        var pngBytes = await _imageClient.GeneratePngAsync(
            prompt,
            ImagePromptDefaults.PillarWidth,
            ImagePromptDefaults.PillarHeight,
            cancellationToken);

        var avifBytes = await FigureAvifEncoder.EncodePngAsync(pngBytes, cancellationToken);

        return await _attach.AttachAvifBytesAsync(
            figure.ProjectId,
            figure.SourceType,
            figure.HeadingSlug,
            avifBytes,
            altOverride: null,
            cancellationToken);
    }
}
