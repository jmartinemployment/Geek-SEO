using ContentWriter.Application.Providers;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace ContentWriter.Application.Services.Figures;

public interface IContentFigureAttachService
{
    Task<ContentFigure> AttachAvifAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        Stream imageStream,
        string fileName,
        string? altOverride = null,
        CancellationToken cancellationToken = default);

    Task<ContentFigure> AttachAvifBytesAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        byte[] avifBytes,
        string? altOverride = null,
        CancellationToken cancellationToken = default);

    Task<ContentFigure> AssignImageUrlAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        string imageUrl,
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
    private readonly SiteImageStorageOptions _storageOptions;
    private readonly BlobStorageOptions _blobOptions;
    private readonly SiteStaticImagePublisher _sitePublisher;
    private readonly VercelBlobUploader _blobUploader;

    public ContentFigureAttachService(
        IContentFigureRepository figures,
        IOptions<SiteImageStorageOptions> storageOptions,
        IOptions<BlobStorageOptions> blobOptions,
        SiteStaticImagePublisher sitePublisher,
        VercelBlobUploader blobUploader)
    {
        _figures = figures;
        _storageOptions = storageOptions.Value;
        _blobOptions = blobOptions.Value;
        _sitePublisher = sitePublisher;
        _blobUploader = blobUploader;
    }

    public async Task<ContentFigure> AttachAvifAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        Stream imageStream,
        string fileName,
        string? altOverride = null,
        CancellationToken cancellationToken = default)
    {
        FigureSourceValidator.ValidateSourceType(sourceType);

        if (!fileName.EndsWith(".avif", StringComparison.OrdinalIgnoreCase))
        {
            throw new ContentGenerationException(
                "Only .avif files are accepted. Export AVIF from Figma before attach.");
        }

        await using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        return await AttachAvifBytesAsync(
            projectId,
            sourceType,
            headingSlug,
            memory.ToArray(),
            altOverride,
            cancellationToken);
    }

    public async Task<ContentFigure> AttachAvifBytesAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        byte[] avifBytes,
        string? altOverride = null,
        CancellationToken cancellationToken = default)
    {
        var figure = await RequireAttachableFigureAsync(projectId, sourceType, headingSlug, cancellationToken);

        using var image = await Image.LoadAsync(new MemoryStream(avifBytes), cancellationToken);

        var storage = _storageOptions.ResolveDefaultStorage(_blobOptions);
        string imageUrl;
        string relativePath;
        string imageStorage;

        if (string.Equals(storage, FigureImageStorage.VercelBlob, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = FigureBlobPathBuilder.BuildBlobPathname(
                figure.GeekApiSlug!,
                headingSlug);
            imageUrl = await _blobUploader.UploadPublicAsync(
                relativePath,
                avifBytes,
                "image/avif",
                _blobOptions.ReadWriteToken,
                cancellationToken);
            imageStorage = FigureImageStorage.VercelBlob;
        }
        else
        {
            relativePath = FigurePublicPathBuilder.BuildRelativePath(
                figure.GeekApiSlug!,
                headingSlug);
            var published = await _sitePublisher.PublishStaticImageAsync(relativePath, avifBytes, cancellationToken);
            imageUrl = published.PublicUrl;
            imageStorage = FigureImageStorage.SiteStatic;
        }

        figure.ImageRelativePath = relativePath;
        figure.ImageUrl = imageUrl;
        figure.ImageStorage = imageStorage;
        figure.ImageWidth = image.Width;
        figure.ImageHeight = image.Height;
        figure.ImageAlt = string.IsNullOrWhiteSpace(altOverride)
            ? FigureHeadingSlugResolver.DefaultImageAlt(figure.Heading)
            : altOverride.Trim();
        figure.Status = FigureStatus.Ready;
        figure.SkipReason = null;
        figure.UpdatedAtUtc = DateTime.UtcNow;

        await _figures.UpdateAsync(figure, cancellationToken);
        await _figures.SaveChangesAsync(cancellationToken);
        return figure;
    }

    public async Task<ContentFigure> AssignImageUrlAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        string imageUrl,
        string? altOverride = null,
        CancellationToken cancellationToken = default)
    {
        FigureSourceValidator.ValidateSourceType(sourceType);

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ContentGenerationException("Image URL is required.");
        }

        var figure = await RequireAttachableFigureAsync(projectId, sourceType, headingSlug, cancellationToken);
        var trimmedUrl = imageUrl.Trim();
        var publicBase = _storageOptions.PublicBaseUrl.TrimEnd('/');

        figure.ImageUrl = trimmedUrl;
        figure.ImageRelativePath = trimmedUrl.StartsWith($"{publicBase}/", StringComparison.OrdinalIgnoreCase)
            ? trimmedUrl[(publicBase.Length + 1)..]
            : null;
        figure.ImageStorage = trimmedUrl.Contains("blob.vercel-storage.com", StringComparison.OrdinalIgnoreCase)
            ? FigureImageStorage.VercelBlob
            : FigureImageStorage.Manual;
        figure.ImageWidth = null;
        figure.ImageHeight = null;
        figure.ImageAlt = string.IsNullOrWhiteSpace(altOverride)
            ? FigureHeadingSlugResolver.DefaultImageAlt(figure.Heading)
            : altOverride.Trim();
        figure.Status = FigureStatus.Ready;
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
        FigureSourceValidator.ValidateSourceType(sourceType);

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

    private async Task<ContentFigure> RequireAttachableFigureAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken)
    {
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

        return figure;
    }
}
