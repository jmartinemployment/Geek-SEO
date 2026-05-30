using System.Net.Http.Json;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpKeywordRepository(IHttpClientFactory factory) : IKeywordRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<IReadOnlyList<SeoKeyword>>> GetByProjectAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/keywords/project/{projectId}", ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoKeyword>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var list = await response.Content.ReadFromJsonAsync<List<SeoKeyword>>(ct);
        return Result<IReadOnlyList<SeoKeyword>>.Success(list ?? []);
    }

    public async Task<Result> BulkUpsertAsync(
        Guid projectId, IReadOnlyList<KeywordResult> keywords, string location, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/seo/internal/keywords/bulk-upsert?projectId={projectId}&location={Uri.EscapeDataString(location)}",
            keywords,
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }
}
