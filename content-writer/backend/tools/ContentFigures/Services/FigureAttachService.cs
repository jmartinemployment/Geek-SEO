using ContentFigures.Infrastructure;
using ContentWriter.Application.Services.Figures;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ContentFigures.Services;

public sealed class FigureAttachService(ContentWriter.Infrastructure.Data.ContentWriterDbContext db)
{
    public Task AttachAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        string imageFilePath,
        string? altOverride,
        CancellationToken cancellationToken = default) =>
        ContentFigureAttachRunner.AttachFileAsync(
            projectId,
            sourceType,
            headingSlug,
            imageFilePath,
            altOverride,
            cancellationToken);

    public async Task SyncDirectoryAsync(
        Guid projectId,
        string sourceType,
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var figures = await db.ContentFigures
            .Where(f => f.ProjectId == projectId && f.SourceType == sourceType)
            .ToListAsync(cancellationToken);

        if (figures.Count == 0)
        {
            throw new InvalidOperationException($"No {sourceType} figures for project {projectId}.");
        }

        var knownSlugs = figures.Select(f => f.HeadingSlug).ToList();
        var files = new DirectoryInfo(directoryPath).GetFiles("h2-*.webp", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            throw new InvalidOperationException($"No h2-*.webp files in {directoryPath}.");
        }

        foreach (var file in files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            var slug = FigureSyncDirMatcher.ResolveHeadingSlug(file.Name, knownSlugs);
            if (slug is null)
            {
                throw new InvalidOperationException(
                    $"Cannot match file {file.Name} to any heading slug for project {projectId}.");
            }

            await ContentFigureAttachRunner.AttachFileAsync(
                projectId,
                sourceType,
                slug,
                file.FullName,
                altOverride: null,
                cancellationToken);
        }
    }
}
