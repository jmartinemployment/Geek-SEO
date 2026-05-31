using System.Net;
using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpPublishedPageRepository(IHttpClientFactory factory, ICurrentUserContext user) : IPublishedPageRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<IReadOnlyList<SeoPublishedPage>>> ListByProjectAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/published-pages?projectId={projectId}&userId={user.UserId}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoPublishedPage>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<SeoPublishedPage>>(ct);
        return Result<IReadOnlyList<SeoPublishedPage>>.Success(value ?? []);
    }

    public async Task<Result<SeoPublishedPage?>> GetByIdAsync(Guid publishedPageId, CancellationToken ct = default)
    {
        _ = publishedPageId;
        return Result<SeoPublishedPage?>.NotFound("Use project list");
    }

    public async Task<Result<IReadOnlyList<PerformanceSnapshotPoint>>> GetSparklineAsync(
        Guid publishedPageId,
        int days,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/published-pages/{publishedPageId}/sparkline?userId={user.UserId}&days={days}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<IReadOnlyList<PerformanceSnapshotPoint>>.Success([]);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<PerformanceSnapshotPoint>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<PerformanceSnapshotPoint>>(ct);
        return Result<IReadOnlyList<PerformanceSnapshotPoint>>.Success(value ?? []);
    }

    public async Task<Result> UpsertSnapshotAsync(SeoContentPerformanceSnapshot snapshot, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/seo/internal/published-pages/snapshots", snapshot, ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<IReadOnlyList<PublishedPageRefreshCandidate>>> ListDueForSnapshotAsync(
        int limit,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/published-pages/maintenance/due?userId={user.UserId}&limit={limit}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<PublishedPageRefreshCandidate>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<PublishedPageRefreshCandidate>>(ct);
        return Result<IReadOnlyList<PublishedPageRefreshCandidate>>.Success(value ?? []);
    }
}
