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
        await using var db = ContentFiguresDb.CreateContext();
        using var http = new HttpClient();
        var imageOptions = Options.Create(new FigureImageGenerationOptions { InAppGenerationEnabled = true });
        var generation = new ContentFigureImageGenerationService(
            new ContentFigureRepository(db),
            ContentFigureAttachServiceFactory.Create(db),
            new OpenAiFigureImageClient(
                http,
                Options.Create(new LlmProvidersOptions()),
                imageOptions),
            imageOptions);

        if (!string.IsNullOrWhiteSpace(headingSlug))
        {
            var single = await generation.GenerateAsync(projectId, sourceType, headingSlug, cancellationToken);
            Console.WriteLine($"  [{single.SectionOrder}] {single.HeadingSlug} -> {single.ImageUrl}");
            return 1;
        }

        var figures = await generation.GeneratePendingAsync(projectId, sourceType, cancellationToken);
        foreach (var figure in figures)
        {
            Console.WriteLine($"  [{figure.SectionOrder}] {figure.HeadingSlug} -> {figure.ImageUrl}");
        }

        return figures.Count;
    }
}
