using System.Net;
using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpGeoTrackingRepository(IHttpClientFactory factory, ICurrentUserContext user) : IGeoTrackingRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<IReadOnlyList<SeoGeoTrackingQuery>>> ListByProjectAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/geo/queries?projectId={projectId}&userId={user.UserId}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoGeoTrackingQuery>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<SeoGeoTrackingQuery>>(ct);
        return Result<IReadOnlyList<SeoGeoTrackingQuery>>.Success(value ?? []);
    }

    public async Task<Result<SeoGeoTrackingQuery>> CreateAsync(SeoGeoTrackingQuery query, CancellationToken ct = default)
    {
        var request = new CreateGeoTrackingQueryRequest
        {
            ProjectId = query.ProjectId,
            QueryText = query.QueryText,
        };
        var response = await _http.PostAsJsonAsync(
            $"api/seo/internal/geo/queries?userId={user.UserId}",
            request,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoGeoTrackingQuery>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoGeoTrackingQuery>(ct);
        return value is null
            ? Result<SeoGeoTrackingQuery>.Failure("Empty response")
            : Result<SeoGeoTrackingQuery>.Success(value);
    }

    public async Task<Result> DeleteAsync(Guid queryId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync(
            $"api/seo/internal/geo/queries/{queryId}?userId={user.UserId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result.Failure("Query not found");
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<SeoGeoTrackingQuery?>> GetQueryAsync(Guid queryId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/geo/queries/{queryId}?userId={user.UserId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoGeoTrackingQuery?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoGeoTrackingQuery?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoGeoTrackingQuery>(ct);
        return Result<SeoGeoTrackingQuery?>.Success(value);
    }

    public async Task<Result<IReadOnlyList<SeoGeoMentionSnapshot>>> ListSnapshotsAsync(
        Guid queryId,
        int days,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/geo/queries/{queryId}/snapshots?userId={user.UserId}&days={days}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoGeoMentionSnapshot>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<SeoGeoMentionSnapshot>>(ct);
        return Result<IReadOnlyList<SeoGeoMentionSnapshot>>.Success(value ?? []);
    }

    public async Task<Result> AddSnapshotAsync(SeoGeoMentionSnapshot snapshot, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/seo/internal/geo/snapshots?userId={user.UserId}",
            snapshot,
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<IReadOnlyList<GeoProbeCandidate>>> ListEnabledQueriesAsync(
        int limit,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/geo/maintenance/enabled-queries?userId={user.UserId}&limit={limit}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<GeoProbeCandidate>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<GeoProbeCandidate>>(ct);
        return Result<IReadOnlyList<GeoProbeCandidate>>.Success(value ?? []);
    }
}
