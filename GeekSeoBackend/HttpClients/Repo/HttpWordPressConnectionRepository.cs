using System.Net.Http.Json;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpWordPressConnectionRepository(IHttpClientFactory factory) : IWordPressConnectionRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoWordPressConnection?>> GetByProjectAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/wordpress/connections?projectId={projectId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result<SeoWordPressConnection?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoWordPressConnection?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var row = await response.Content.ReadFromJsonAsync<SeoWordPressConnection>(ct);
        return Result<SeoWordPressConnection?>.Success(row);
    }

    public async Task<Result<SeoWordPressConnection>> UpsertAsync(
        SeoWordPressConnection connection, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/seo/internal/wordpress/connections", connection, ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoWordPressConnection>.Failure(await response.Content.ReadAsStringAsync(ct));
        var row = await response.Content.ReadFromJsonAsync<SeoWordPressConnection>(ct);
        return row is null
            ? Result<SeoWordPressConnection>.Failure("Empty response")
            : Result<SeoWordPressConnection>.Success(row);
    }

    public async Task<Result> DeleteByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/seo/internal/wordpress/connections?projectId={projectId}", ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }
}
