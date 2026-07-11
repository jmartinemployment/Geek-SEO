using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Entities;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Data;
using ContentFigures.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace ContentFigures.Services;

public sealed class FigureAttachService(
    ContentWriterDbContext db,
    VercelBlobUploader blobUploader)
{
    public async Task<ContentFigure> AttachAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        string imageFilePath,
        string? altOverride,
        string blobToken,
        CancellationToken cancellationToken = default)
    {
        ValidateSourceType(sourceType);

        if (!File.Exists(imageFilePath))
        {
            throw new FileNotFoundException($"Image file not found: {imageFilePath}");
        }

        var extension = Path.GetExtension(imageFilePath);
        if (!extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only .webp files are accepted. Export WebP from Figma before attach.");
        }

        var figure = await db.ContentFigures.FirstOrDefaultAsync(
            f => f.ProjectId == projectId
                 && f.SourceType == sourceType
                 && f.HeadingSlug == headingSlug,
            cancellationToken);

        if (figure is null)
        {
            throw new InvalidOperationException(
                $"No figure row for project {projectId}, source={sourceType}, headingSlug={headingSlug}.");
        }

        if (string.IsNullOrWhiteSpace(figure.GeekApiSlug))
        {
            throw new InvalidOperationException(
                "GeekApiSlug is missing. Publish text to geekatyourspot first, then attach art.");
        }

        if (figure.Status == FigureStatus.Skipped)
        {
            throw new InvalidOperationException(
                $"Figure {headingSlug} is Skipped. Attach is not allowed on skipped sections.");
        }

        var bytes = await File.ReadAllBytesAsync(imageFilePath, cancellationToken);
        using var image = await Image.LoadAsync(imageFilePath, cancellationToken);

        var pathname = FigureBlobPathBuilder.BuildBlobPathname(
            figure.GeekApiSlug,
            sourceType,
            headingSlug);

        var imageUrl = await blobUploader.UploadPublicAsync(
            pathname,
            bytes,
            "image/webp",
            blobToken,
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

        await db.SaveChangesAsync(cancellationToken);
        return figure;
    }

    public static void ValidateSourceType(string sourceType)
    {
        if (sourceType is not (FigureSourceType.Pillar or FigureSourceType.Blog))
        {
            throw new ArgumentException(
                $"Source must be '{FigureSourceType.Pillar}' or '{FigureSourceType.Blog}'.",
                nameof(sourceType));
        }
    }
}
