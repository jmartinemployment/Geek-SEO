using ContentWriter.Application.Providers;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace ContentWriter.Application.Services.Figures;

public interface IContentFigureAttachService
{
    Task<ContentFigure> AttachWebpAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        Stream imageStream,
        string fileName,
        string? altOverride = null,
        CancellationToken cancellationToken = default);

    Task<ContentFigure> AttachWebpBytesAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        byte[] webpBytes,
        string? altOverride = null,
        CancellationToken cancellationToken = default);

    Task<ContentFigure> SkipAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default);
}

public sealed class ContentFigureAttachService : IContentFigureAttachService
{
    private readonly IContentFigureRepository _figures;
    private readonly BlobStorageOptions _blobOptions;
    private readonly VercelBlobUploader _blobUploader;

    public ContentFigureAttachService(
        IContentFigureRepository figures,
        IOptions<BlobStorageOptions> blobOptions,
        VercelBlobUploader blobUploader)
    {
        _figures = figures;
        _blobOptions = blobOptions.Value;
        _blobUploader = blobUploader;
    }

    public async Task<ContentFigure> AttachWebpAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        Stream imageStream,
        string fileName,
        string? altOverride = null,
        CancellationToken cancellationToken = default)
    {
        FigureMergeService.ValidateSourceType(sourceType);

        if (!fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            throw new ContentGenerationException(
                "Only .webp files are accepted. Export WebP from Figma before attach.");
        }

        var figure = await _figures.GetByHeadingSlugAsync(projectId, sourceType, headingSlug, cancellationToken);
        if (figure is null)
        {
            throw new ContentGenerationException(
                $"No figure row for source={sourceType}, headingSlug={headingSlug}.");
        }

        if (string.IsNullOrWhiteSpace(figure.GeekApiSlug))
        {
            throw new ContentGenerationException(
                "Publish text to geekatyourspot first, then attach art.");
        }

        if (figure.Status == FigureStatus.Skipped)
        {
            throw new ContentGenerationException(
                $"Figure {headingSlug} is Skipped. Attach is not allowed on skipped sections.");
        }

        await using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        return await AttachWebpBytesAsync(
            projectId,
            sourceType,
            headingSlug,
            memory.ToArray(),
            altOverride,
            cancellationToken);
    }

    public async Task<ContentFigure> AttachWebpBytesAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        byte[] webpBytes,
        string? altOverride = null,
        CancellationToken cancellationToken = default)
    {
        FigureMergeService.ValidateSourceType(sourceType);

        var figure = await _figures.GetByHeadingSlugAsync(projectId, sourceType, headingSlug, cancellationToken);
        if (figure is null)
        {
            throw new ContentGenerationException(
                $"No figure row for source={sourceType}, headingSlug={headingSlug}.");
        }

        if (string.IsNullOrWhiteSpace(figure.GeekApiSlug))
        {
            throw new ContentGenerationException(
                "Publish text to geekatyourspot first, then attach art.");
        }

        if (figure.Status == FigureStatus.Skipped)
        {
            throw new ContentGenerationException(
                $"Figure {headingSlug} is Skipped. Attach is not allowed on skipped sections.");
        }

        using var image = await Image.LoadAsync(new MemoryStream(webpBytes), cancellationToken);

        var pathname = FigureBlobPathBuilder.BuildBlobPathname(
            figure.GeekApiSlug,
            sourceType,
            headingSlug);

        var imageUrl = await _blobUploader.UploadPublicAsync(
            pathname,
            webpBytes,
            "image/webp",
            _blobOptions.ReadWriteToken,
            cancellationToken);

        figure.ImageUrl = imageUrl;
        figure.ImageWidth = image.Width;
        figure.ImageHeight = image.Height;
        figure.ImageAlt = string.IsNullOrWhiteSpace(altOverride)
            ? FigureHeadingSlugResolver.DefaultImageAlt(figure.Heading)
            : altOverride.Trim();
        figure.Status = FigureStatus.Ready;
        figure.NeedsFigureMerge = true;
        figure.SkipReason = null;
        figure.UpdatedAtUtc = DateTime.UtcNow;

        await _figures.UpdateAsync(figure, cancellationToken);
        await _figures.SaveChangesAsync(cancellationToken);
        return figure;
    }

    public async Task<ContentFigure> SkipAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default)
    {
        FigureMergeService.ValidateSourceType(sourceType);

        var figure = await _figures.GetByHeadingSlugAsync(projectId, sourceType, headingSlug, cancellationToken);
        if (figure is null)
        {
            throw new ContentGenerationException(
                $"No figure row for source={sourceType}, headingSlug={headingSlug}.");
        }

        figure.Status = FigureStatus.Skipped;
        figure.SkipReason = FigureSkipReason.UserSkipped;
        figure.NeedsFigureMerge = false;
        figure.UpdatedAtUtc = DateTime.UtcNow;

        await _figures.UpdateAsync(figure, cancellationToken);
        await _figures.SaveChangesAsync(cancellationToken);
        return figure;
    }
}
