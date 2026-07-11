using ContentWriter.Application.Services.Figures;
using ContentWriter.Application.Services.Publish;
using ContentWriter.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ContentFigures.Infrastructure;

public static class FigureMergeRunner
{
    public static async Task<FigureMergeResult> MergeAsync(
        Guid projectId,
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("GEEK_BACKEND_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GEEK_BACKEND_API_KEY is required for merge.");
        }

        var options = Options.Create(new GeekBlogPublishOptions
        {
            ApiUrl = Environment.GetEnvironmentVariable("GEEK_API_URL")
                     ?? "https://api.geekatyourspot.com",
            ApiKey = apiKey,
            SiteBaseUrl = Environment.GetEnvironmentVariable("GEEK_SITE_BASE_URL")
                          ?? "https://www.geekatyourspot.com",
            RevalidateSecret = Environment.GetEnvironmentVariable("REVALIDATE_SECRET") ?? string.Empty,
        });

        await using var db = ContentFiguresDb.CreateContext();
        var httpFactory = new SimpleHttpClientFactory();
        var merge = new FigureMergeService(
            new ContentFigureRepository(db),
            options,
            httpFactory,
            NullLogger<FigureMergeService>.Instance);

        return await merge.MergeSourceAsync(projectId, sourceType, cancellationToken);
    }

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
