using ContentWriter.Application.Services.Figures;
using ContentWriter.Domain.Enums;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContentFigures.Infrastructure;

public static class ContentImagePurgeRunner
{
    public static async Task<int> PurgeVercelBlobsAsync(
        string? prefix,
        CancellationToken cancellationToken = default)
    {
        var token = Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Set BLOB_READ_WRITE_TOKEN to delete blobs from Vercel Storage.");
        }

        using var http = new HttpClient();
        var store = new VercelBlobStore(http);
        return await store.DeleteAllAsync(token, prefix ?? "images/", cancellationToken);
    }

    public static int PurgeLocalSiteImages(string? outputRoot = null)
    {
        var root = outputRoot ?? SiteImageStorageOptionsFactory.Create().ResolveLocalOutputRoot();
        var imagesRoot = Path.Combine(root, "images");
        if (!Directory.Exists(imagesRoot))
        {
            return 0;
        }

        var deleted = 0;
        var targets = new[]
        {
            Path.Combine(imagesRoot, "TechnicalArticle"),
            Path.Combine(imagesRoot, "Blog"),
            Path.Combine(imagesRoot, "Tool"),
            Path.Combine(imagesRoot, "content"),
        };

        foreach (var dir in targets)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                    && !file.EndsWith(".avif", StringComparison.OrdinalIgnoreCase)
                    && !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    && !file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    && !file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(file);
                deleted++;
            }

            PruneEmptyDirectories(dir);
        }

        return deleted;
    }

    public static async Task<int> ClearFigureImageRefsAsync(
        Guid? projectId,
        CancellationToken cancellationToken = default)
    {
        await using var db = ContentFiguresDb.CreateContext();
        var query = db.ContentFigures.AsQueryable();
        if (projectId is not null)
        {
            query = query.Where(f => f.ProjectId == projectId);
        }

        var figures = await query
            .Where(f => f.ImageUrl != null || f.ImageRelativePath != null)
            .ToListAsync(cancellationToken);

        foreach (var figure in figures)
        {
            figure.ImageUrl = null;
            figure.ImageRelativePath = null;
            figure.ImageWidth = null;
            figure.ImageHeight = null;
            figure.ImageStorage = FigureImageStorage.SiteStatic;
            if (figure.Status == FigureStatus.Ready || figure.Status == FigureStatus.Published)
            {
                figure.Status = FigureStatus.Pending;
            }

            figure.NeedsFigureMerge = false;
            figure.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (figures.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return figures.Count;
    }

    private static void PruneEmptyDirectories(string root)
    {
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }
    }
}
