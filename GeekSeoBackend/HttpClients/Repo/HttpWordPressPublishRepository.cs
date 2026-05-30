using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpWordPressPublishRepository(IHttpClientFactory factory) : IWordPressPublishRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result> RecordPublishAsync(
        Guid projectId,
        Guid documentId,
        string targetKeyword,
        int wordCount,
        string title,
        string publishedUrl,
        int wordPressPostId,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/seo/internal/wordpress/publish-log",
            new
            {
                projectId,
                documentId,
                targetKeyword,
                wordCount,
                title,
                publishedUrl,
                wordPressPostId,
            },
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }
}
