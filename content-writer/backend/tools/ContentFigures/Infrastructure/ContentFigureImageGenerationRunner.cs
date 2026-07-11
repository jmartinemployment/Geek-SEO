using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Figures;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace ContentFigures.Infrastructure;

public static class ContentFigureImageGenerationRunner
{
    public static async Task<int> GenerateAsync(
        Guid projectId,
        string sourceType,
        string? headingSlug,
        CancellationToken cancellationToken = default)
    {
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(openAiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is required for generate.");
        }

        var blobToken = Environment.GetEnvironmentVariable("BLOB_READ_WRITE_TOKEN");
        if (string.IsNullOrWhiteSpace(blobToken))
        {
            throw new InvalidOperationException("BLOB_READ_WRITE_TOKEN is required for generate.");
        }

        await using var db = ContentFiguresDb.CreateContext();
        var llmOptions = Options.Create(new LlmProvidersOptions
        {
            OpenAi = new OpenAiOptions { ApiKey = openAiKey },
        });
        var blobOptions = Options.Create(new BlobStorageOptions { ReadWriteToken = blobToken });
        var imageOptions = Options.Create(new FigureImageGenerationOptions());

        var generation = new ContentFigureImageGenerationService(
            new ContentFigureRepository(db),
            new ContentFigureAttachService(
                new ContentFigureRepository(db),
                blobOptions,
                new VercelBlobUploader(new HttpClient())),
            new OpenAiFigureImageClient(new HttpClient(), llmOptions, imageOptions));

        if (string.IsNullOrWhiteSpace(headingSlug))
        {
            var figures = await generation.GeneratePendingAsync(projectId, sourceType, cancellationToken);
            foreach (var figure in figures)
            {
                Console.WriteLine($"  [{figure.SectionOrder}] {figure.HeadingSlug} -> {figure.ImageUrl}");
            }

            return figures.Count;
        }

        var single = await generation.GenerateAsync(projectId, sourceType, headingSlug, cancellationToken);
        Console.WriteLine($"  [{single.SectionOrder}] {single.HeadingSlug} -> {single.ImageUrl}");
        return 1;
    }
}
