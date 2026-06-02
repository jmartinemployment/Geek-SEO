using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpNicheProfileRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : INicheProfileRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<NicheProfile>> CreateAsync(NicheProfile profile, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles?userId={user.UserId}", profile, ct);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfile>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfile>(Json, ct);
        return value is null
            ? Result<NicheProfile>.Failure("Empty response")
            : Result<NicheProfile>.Success(value);
    }

    public async Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/{profileId}?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<NicheProfile?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfile?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfile?>(Json, ct);
        return Result<NicheProfile?>.Success(value);
    }

    public async Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/project/{projectId}/latest?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<NicheProfile?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfile?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfile?>(Json, ct);
        return Result<NicheProfile?>.Success(value);
    }

    public async Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/project/{projectId}/history?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result> UpdateStatusAsync(
        Guid profileId, string status, string? step = null,
        int stepNumber = 0, int totalSteps = 0, string? errorMessage = null,
        CancellationToken ct = default)
    {
        var body = new { status, step, stepNumber, totalSteps, errorMessage };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/status?userId={user.UserId}", body, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> UpdateScoresAsync(
        Guid profileId, decimal authorityScore, int covered, int partial, int gap,
        CancellationToken ct = default)
    {
        var body = new { authorityScore, covered, partial, gap };
        var res = await _http.PatchAsJsonAsync(
            $"api/seo/internal/niche-profiles/{profileId}/scores?userId={user.UserId}", body, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/pillars?userId={user.UserId}", pillars, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/subtopics?userId={user.UserId}", subtopics, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertCompetitorsAsync(IEnumerable<NicheCompetitor> competitors, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/competitors?userId={user.UserId}", competitors, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/entities?userId={user.UserId}", entities, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/niche-profiles/pillar-pages?userId={user.UserId}", pages, ct);
        return res.IsSuccessStatusCode ? Result.Success() : Result.Failure(await res.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<IReadOnlyList<NicheProfileSummary>>> ListDueForReanalysisAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/maintenance/due?limit={limit}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheProfileSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheProfileSummary>>(Json, ct);
        return Result<IReadOnlyList<NicheProfileSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<NicheQueuedJob>>> ListQueuedAsync(
        int limit, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-profiles/maintenance/queued?limit={limit}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<NicheQueuedJob>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<NicheQueuedJob>>(Json, ct);
        return Result<IReadOnlyList<NicheQueuedJob>>.Success(value ?? []);
    }
}
