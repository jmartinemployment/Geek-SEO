using System.Net;
using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpContentGuardRepository(IHttpClientFactory factory, ICurrentUserContext user) : IContentGuardRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoContentGuardPolicy?>> GetPolicyAsync(Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/content-guard/{projectId}/policy?userId={user.UserId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoContentGuardPolicy?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoContentGuardPolicy?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoContentGuardPolicy>(ct);
        return Result<SeoContentGuardPolicy?>.Success(value);
    }

    public async Task<Result<SeoContentGuardPolicy>> UpsertPolicyAsync(
        SeoContentGuardPolicy policy,
        CancellationToken ct = default)
    {
        var request = new UpsertContentGuardPolicyRequest
        {
            Enabled = policy.Enabled,
            AutoPatch = policy.AutoPatch,
        };
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/content-guard/{policy.ProjectId}/policy?userId={user.UserId}",
            request,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoContentGuardPolicy>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoContentGuardPolicy>(ct);
        return value is null
            ? Result<SeoContentGuardPolicy>.Failure("Empty response")
            : Result<SeoContentGuardPolicy>.Success(value);
    }

    public async Task<Result<IReadOnlyList<SeoContentGuardRun>>> ListRunsAsync(
        Guid projectId,
        int limit,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/content-guard/{projectId}/runs?userId={user.UserId}&limit={limit}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoContentGuardRun>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<SeoContentGuardRun>>(ct);
        return Result<IReadOnlyList<SeoContentGuardRun>>.Success(value ?? []);
    }

    public async Task<Result<SeoContentGuardRun>> CreateRunAsync(SeoContentGuardRun run, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/seo/internal/content-guard/runs?userId={user.UserId}",
            run,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoContentGuardRun>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoContentGuardRun>(ct);
        return value is null
            ? Result<SeoContentGuardRun>.Failure("Empty response")
            : Result<SeoContentGuardRun>.Success(value);
    }

    public async Task<Result<SeoContentGuardRun>> UpdateRunAsync(SeoContentGuardRun run, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/content-guard/runs/{run.Id}?userId={user.UserId}",
            run,
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoContentGuardRun>.NotFound("Run not found");
        if (!response.IsSuccessStatusCode)
            return Result<SeoContentGuardRun>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoContentGuardRun>(ct);
        return value is null
            ? Result<SeoContentGuardRun>.Failure("Empty response")
            : Result<SeoContentGuardRun>.Success(value);
    }

    public async Task<Result<SeoContentGuardRun?>> GetRunAsync(Guid runId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/content-guard/runs/{runId}?userId={user.UserId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoContentGuardRun?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoContentGuardRun?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoContentGuardRun>(ct);
        return Result<SeoContentGuardRun?>.Success(value);
    }

    public async Task<Result<IReadOnlyList<ContentGuardScanCandidate>>> ListProjectsForDailyScanAsync(
        int limit,
        CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/content-guard/maintenance/daily-scan?userId={user.UserId}&limit={limit}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<ContentGuardScanCandidate>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<ContentGuardScanCandidate>>(ct);
        return Result<IReadOnlyList<ContentGuardScanCandidate>>.Success(value ?? []);
    }
}
