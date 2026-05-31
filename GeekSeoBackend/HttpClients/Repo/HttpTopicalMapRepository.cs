using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpTopicalMapRepository(IHttpClientFactory factory, ICurrentUserContext user) : ITopicalMapRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoTopicalMap?>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/topical-maps/{projectId}?userId={user.UserId}",
            ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<SeoTopicalMap?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoTopicalMap?>.Failure(await response.Content.ReadAsStringAsync(ct));

        var raw = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
            return Result<SeoTopicalMap?>.Success(null);

        try
        {
            var value = JsonSerializer.Deserialize<SeoTopicalMap>(raw);
            return Result<SeoTopicalMap?>.Success(value);
        }
        catch (JsonException ex)
        {
            return Result<SeoTopicalMap?>.Failure($"Invalid topical map response: {ex.Message}");
        }
    }

    public async Task<Result<SeoTopicalMap>> UpsertAsync(SeoTopicalMap map, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/topical-maps/{map.ProjectId}?userId={user.UserId}",
            map,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoTopicalMap>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoTopicalMap>(ct);
        return value is null
            ? Result<SeoTopicalMap>.Failure("Empty response")
            : Result<SeoTopicalMap>.Success(value);
    }

    public async Task<Result<IReadOnlyList<TopicalMapRefreshCandidate>>> ListDueForRefreshAsync(
        int limit,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/topical-maps/maintenance/due?userId={user.UserId}&limit={limit}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<TopicalMapRefreshCandidate>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<TopicalMapRefreshCandidate>>(ct);
        return Result<IReadOnlyList<TopicalMapRefreshCandidate>>.Success(value ?? []);
    }
}
