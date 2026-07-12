using ContentWriter.Application.Providers;
using ContentWriter.Application.Services;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace ContentWriter.Application.Services.Figures;

public interface IContentFigureImageGenerationService
{
    Task<ContentFigure> GenerateAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContentFigure>> GeneratePendingAsync(
        Guid projectId,
        string sourceType,
        CancellationToken cancellationToken = default);
}

public sealed class ContentFigureImageGenerationService : IContentFigureImageGenerationService
{
    private readonly IContentFigureRepository _figures;
    private readonly IContentFigureAttachService _attach;
    private readonly OpenAiFigureImageClient _imageClient;
    private readonly FigureImageGenerationOptions _options;

    public ContentFigureImageGenerationService(
        IContentFigureRepository figures,
        IContentFigureAttachService attach,
        OpenAiFigureImageClient imageClient,
        IOptions<FigureImageGenerationOptions> options)
    {
        _figures = figures;
        _attach = attach;
        _imageClient = imageClient;
        _options = options.Value;
    }

    public async Task<ContentFigure> GenerateAsync(
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

        return await GenerateFigureAsync(figure, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentFigure>> GeneratePendingAsync(
        Guid projectId,
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        FigureSourceValidator.ValidateSourceType(sourceType);

        var rows = await _figures.ListByProjectAsync(projectId, cancellationToken);
        var pending = rows
            .Where(f =>
                string.Equals(f.SourceType, sourceType, StringComparison.OrdinalIgnoreCase) &&
                f.Status == FigureStatus.Pending &&
                !string.IsNullOrWhiteSpace(f.BriefText))
            .OrderBy(f => f.SectionOrder)
            .ToList();

        if (pending.Count == 0)
        {
            throw new ContentGenerationException(
                $"No pending {sourceType} figures with briefs to generate.");
        }

        var results = new List<ContentFigure>();
        foreach (var figure in pending)
        {
            results.Add(await GenerateFigureAsync(figure, cancellationToken));
        }

        return results;
    }

    private async Task<ContentFigure> GenerateFigureAsync(
        ContentFigure figure,
        CancellationToken cancellationToken)
    {
        if (!_options.InAppGenerationEnabled)
        {
            throw new ContentGenerationException(
                "In-app image generation is disabled. Copy the figure brief, create art in your external tool, then upload AVIF.");
        }

        if (figure.Status == FigureStatus.Skipped)
        {
            throw new ContentGenerationException(
                $"Figure {figure.HeadingSlug} is Skipped.");
        }

        if (string.IsNullOrWhiteSpace(figure.GeekApiSlug))
        {
            throw new ContentGenerationException(
                "Publish text to geekatyourspot first, then generate art.");
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
