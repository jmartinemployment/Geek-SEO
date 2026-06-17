using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpUrlResearchRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : IUrlResearchRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public async Task<Result<SeoUrlResearch>> CreateQueuedAsync(
        Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/url-research/queued?userId={userId}", request, Json, ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<SeoUrlResearch>> GetFullAsync(Guid urlResearchId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/url-research/{urlResearchId}/full?userId={user.UserId}", ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/url-research?userId={user.UserId}&projectId={projectId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<UrlResearchSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var list = await res.Content.ReadFromJsonAsync<List<UrlResearchSummary>>(Json, ct);
        return Result<IReadOnlyList<UrlResearchSummary>>.Success(list ?? []);
    }

    public async Task<Result<SeoUrlResearch>> PersistFullAsync(
        Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/url-research/{urlResearchId}/full?userId={user.UserId}", body, Json, ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<SeoUrlResearch>> UpdateStatusAsync(
        Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/url-research/{urlResearchId}/status?userId={user.UserId}", patch, Json, ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<IReadOnlyList<UrlResearchQueuedJob>>> ListQueuedAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/url-research/maintenance/queued?limit={limit}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<UrlResearchQueuedJob>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var list = await res.Content.ReadFromJsonAsync<List<UrlResearchQueuedJob>>(Json, ct);
        return Result<IReadOnlyList<UrlResearchQueuedJob>>.Success(list ?? []);
    }

    public async Task<Result<int>> FailStaleRunningAsync(TimeSpan maxAge, CancellationToken ct = default)
    {
        var minutes = Math.Clamp((int)Math.Ceiling(maxAge.TotalMinutes), 1, 60);
        var res = await _http.PostAsync(
            $"api/seo/internal/url-research/maintenance/fail-stale-running?maxAgeMinutes={minutes}&userId={user.UserId}",
            null,
            ct);
        if (!res.IsSuccessStatusCode)
            return Result<int>.Failure(await res.Content.ReadAsStringAsync(ct));
        var payload = await res.Content.ReadFromJsonAsync<FailStaleResponse>(Json, ct);
        return Result<int>.Success(payload?.FailedCount ?? 0);
    }

    public async Task<Result<bool>> TryClaimRunningAsync(Guid urlResearchId, CancellationToken ct = default)
    {
        var res = await _http.PatchAsync(
            $"api/seo/internal/url-research/maintenance/{urlResearchId}/claim-running?userId={user.UserId}",
            null,
            ct);
        if (res.StatusCode == HttpStatusCode.Conflict)
            return Result<bool>.Success(false);
        if (!res.IsSuccessStatusCode)
            return Result<bool>.Failure(await res.Content.ReadAsStringAsync(ct));
        var payload = await res.Content.ReadFromJsonAsync<ClaimResponse>(Json, ct);
        return Result<bool>.Success(payload?.Claimed == true);
    }

    private static async Task<Result<SeoUrlResearch>> ReadOneAsync(HttpResponseMessage res, CancellationToken ct)
    {
        if (res.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoUrlResearch>.NotFound("Page research not found");
        if (res.StatusCode == HttpStatusCode.Forbidden)
            return Result<SeoUrlResearch>.Failure("Access denied");
        if (!res.IsSuccessStatusCode)
            return Result<SeoUrlResearch>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<SeoUrlResearch>(Json, ct);
        return value is null
            ? Result<SeoUrlResearch>.Failure("Empty response")
            : Result<SeoUrlResearch>.Success(value);
    }

    private sealed record FailStaleResponse(int FailedCount);
    private sealed record ClaimResponse(bool Claimed);
}
