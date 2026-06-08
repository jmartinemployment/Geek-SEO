using System.Net;
using System.Net.Http.Json;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpBackgroundJobRepository(IHttpClientFactory factory, ICurrentUserContext user) : IBackgroundJobRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoBackgroundJob>> CreateAsync(CreateBackgroundJobRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/seo/internal/jobs", request, ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoBackgroundJob>.Failure(await response.Content.ReadAsStringAsync(ct));
        var job = await response.Content.ReadFromJsonAsync<SeoBackgroundJob>(ct);
        return job is null
            ? Result<SeoBackgroundJob>.Failure("Empty response")
            : Result<SeoBackgroundJob>.Success(job);
    }

    public async Task<Result<SeoBackgroundJob>> GetByIdAsync(Guid jobId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/jobs/{jobId}/entity?userId={user.UserId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoBackgroundJob>.NotFound("Job not found");
        if (!response.IsSuccessStatusCode)
            return Result<SeoBackgroundJob>.Failure(await response.Content.ReadAsStringAsync(ct));
        var job = await response.Content.ReadFromJsonAsync<SeoBackgroundJob>(ct);
        return job is null ? Result<SeoBackgroundJob>.Failure("Empty response") : Result<SeoBackgroundJob>.Success(job);
    }

    public async Task<Result> UpdateProgressAsync(Guid jobId, int progressPercent, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/jobs/{jobId}/progress",
            new { progressPercent },
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> MarkCompleteAsync(Guid jobId, Guid? resultId, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/jobs/{jobId}/complete",
            new { resultId },
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> MarkFailedAsync(Guid jobId, string errorMessage, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/jobs/{jobId}/failed",
            new { errorMessage },
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result<IReadOnlyList<SeoBackgroundJob>>> GetPendingAsync(
        string jobType, int limit, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/jobs/pending?jobType={Uri.EscapeDataString(jobType)}&limit={limit}&userId={user.UserId}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoBackgroundJob>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var list = await response.Content.ReadFromJsonAsync<List<SeoBackgroundJob>>(ct);
        return Result<IReadOnlyList<SeoBackgroundJob>>.Success(list ?? []);
    }
}
