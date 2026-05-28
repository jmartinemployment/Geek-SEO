using GeekSeoBackend.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using GeekSeoBackend.Auth;
using GeekApplication.Entities.Seo;
using GeekApplication.Interfaces.Seo;
using GeekApplication.Models.Seo;
using GeekApplication.Results;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpProjectRepository(IHttpClientFactory factory, ICurrentUserContext user) : IProjectRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<IReadOnlyList<SeoProject>>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/projects?userId={userId}", ct);
        return await ReadListAsync<SeoProject>(response, ct);
    }

    public async Task<Result<SeoProject>> GetByIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/projects/{projectId}?userId={user.UserId}", ct);
        return await ReadOneAsync<SeoProject>(response, ct);
    }

    public async Task<Result<SeoProject>> GetByIdAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/projects/{projectId}?userId={userId}", ct);
        return await ReadOneAsync<SeoProject>(response, ct);
    }

    public async Task<Result<SeoProject>> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/seo/internal/projects?userId={userId}", request, ct);
        return await ReadOneAsync<SeoProject>(response, ct);
    }

    public async Task<Result<SeoProject>> UpdateAsync(Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/seo/internal/projects/{projectId}?userId={user.UserId}", request, ct);
        return await ReadOneAsync<SeoProject>(response, ct);
    }

    public async Task<Result> DeleteAsync(Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/seo/internal/projects/{projectId}?userId={user.UserId}", ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<Result<T>> ReadOneAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<T>.NotFound("Not found");
        if (!response.IsSuccessStatusCode)
            return Result<T>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<T>(ct);
        return value is null ? Result<T>.Failure("Empty response") : Result<T>.Success(value);
    }

    private static async Task<Result<IReadOnlyList<T>>> ReadListAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<T>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<T>>(ct);
        return Result<IReadOnlyList<T>>.Success(value ?? []);
    }
}
