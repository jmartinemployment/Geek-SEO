using ContentWriter.Application.Services.Figures;
using ContentWriter.Application.Services.Publish;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
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
        var blobToken = Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN");
        if (string.IsNullOrWhiteSpace(blobToken))
        {
            throw new InvalidOperationException(
                "BLOB_READ_WRITE_TOKEN is required for attach and sync-dir.");
        }

        await using var db = ContentFiguresDb.CreateContext();
        await using var stream = File.OpenRead(imageFilePath);
        var attach = CreateAttachService(db, blobToken);

        await attach.AttachWebpAsync(
            projectId,
            sourceType,
            headingSlug,
            stream,
            Path.GetFileName(imageFilePath),
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
        var attach = CreateAttachService(db, blobToken: string.Empty);
        await attach.SkipAsync(projectId, sourceType, headingSlug, cancellationToken);
    }

    private static ContentFigureAttachService CreateAttachService(
        ContentWriter.Infrastructure.Data.ContentWriterDbContext db,
        string blobToken)
    {
        var options = Options.Create(new BlobStorageOptions { ReadWriteToken = blobToken });
        return new ContentFigureAttachService(
            new ContentFigureRepository(db),
            options,
            new VercelBlobUploader(new HttpClient()));
    }
}
