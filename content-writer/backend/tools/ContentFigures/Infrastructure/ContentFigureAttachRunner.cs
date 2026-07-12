using ContentWriter.Application.Services.Figures;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace ContentFigures.Infrastructure;

public static class ContentFigureAttachRunner
{
    public static async Task AttachFileAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        string imageFilePath,
        string? altOverride,
        CancellationToken cancellationToken = default)
    {
        await using var db = ContentFiguresDb.CreateContext();
        await using var stream = File.OpenRead(imageFilePath);
        var attach = CreateAttachService(db);

        await attach.AttachWebpAsync(
            projectId,
            sourceType,
            headingSlug,
            stream,
            Path.GetFileName(imageFilePath),
            altOverride,
            cancellationToken);
    }

    public static async Task AssignUrlAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        string imageUrl,
        string? altOverride,
        CancellationToken cancellationToken = default)
    {
        await using var db = ContentFiguresDb.CreateContext();
        var attach = CreateAttachService(db);
        await attach.AssignImageUrlAsync(
            projectId,
            sourceType,
            headingSlug,
            imageUrl,
            altOverride,
            cancellationToken);
    }

    public static async Task SkipAsync(
        Guid projectId,
        string sourceType,
        string headingSlug,
        CancellationToken cancellationToken = default)
    {
        await using var db = ContentFiguresDb.CreateContext();
        var attach = CreateAttachService(db);
        await attach.SkipAsync(projectId, sourceType, headingSlug, cancellationToken);
    }

    private static ContentFigureAttachService CreateAttachService(
        ContentWriter.Infrastructure.Data.ContentWriterDbContext db)
        => ContentFigureAttachServiceFactory.Create(db);
}
