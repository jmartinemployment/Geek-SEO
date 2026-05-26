using System.Net;
using System.Net.Http.Json;
using GeekApplication.Entities.Seo;
using GeekApplication.Interfaces.Seo;
using GeekApplication.Models.Seo;
using GeekApplication.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpBrandVoiceRepository(IHttpClientFactory factory, ICurrentUserContext user) : IBrandVoiceRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<IReadOnlyList<SeoBrandVoice>>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/brand-voices?userId={userId}", ct);
        return await ReadListAsync<SeoBrandVoice>(response, ct);
    }

    public async Task<Result<SeoBrandVoice>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/brand-voices/{id}?userId={user.UserId}", ct);
        return await ReadOneAsync<SeoBrandVoice>(response, ct);
    }

    public async Task<Result<SeoBrandVoice>> CreateAsync(
        Guid userId, CreateBrandVoiceRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/seo/internal/brand-voices?userId={userId}", request, ct);
        return await ReadOneAsync<SeoBrandVoice>(response, ct);
    }

    public async Task<Result<SeoBrandVoice>> UpdateAsync(
        Guid userId, Guid id, UpdateBrandVoiceRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/brand-voices/{id}?userId={userId}",
            request,
            ct);
        return await ReadOneAsync<SeoBrandVoice>(response, ct);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/seo/internal/brand-voices/{id}?userId={userId}", ct);
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
        var list = await response.Content.ReadFromJsonAsync<List<T>>(ct);
        return Result<IReadOnlyList<T>>.Success(list ?? []);
    }
}
