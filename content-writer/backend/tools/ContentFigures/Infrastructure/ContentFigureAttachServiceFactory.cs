using ContentWriter.Application.Services.Figures;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace ContentFigures.Infrastructure;

internal static class ContentFigureAttachServiceFactory
{
    public static ContentFigureAttachService Create(ContentWriter.Infrastructure.Data.ContentWriterDbContext db)
    {
        var storageOptions = Options.Create(SiteImageStorageOptionsFactory.Create());
        var blobOptions = Options.Create(new BlobStorageOptions
        {
            ReadWriteToken = Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN") ?? string.Empty,
        });

        return new ContentFigureAttachService(
            new ContentFigureRepository(db),
            storageOptions,
            blobOptions,
            new SiteStaticImagePublisher(storageOptions.Value),
            new VercelBlobUploader(new HttpClient()));
    }
}
