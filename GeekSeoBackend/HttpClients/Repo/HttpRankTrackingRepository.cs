using System.Net.Http.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpRankTrackingRepository(
    IHttpClientFactory factory,
    ILogger<HttpRankTrackingRepository> logger) : IRankTrackingRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<IReadOnlyList<SeoTrackedKeyword>>> GetKeywordsAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"api/seo/internal/rank-tracking?projectId={projectId:D}";
            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyList<SeoTrackedKeyword>>.Failure(
                    $"HTTP {(int)response.StatusCode}");

            var value = await response.Content.ReadFromJsonAsync<List<SeoTrackedKeyword>>(cancellationToken: ct);
            return Result<IReadOnlyList<SeoTrackedKeyword>>.Success(value ?? []);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get tracked keywords for project {ProjectId}", projectId);
            return Result<IReadOnlyList<SeoTrackedKeyword>>.Failure(ex.Message);
        }
    }

    public async Task<Result<SeoTrackedKeyword>> AddKeywordAsync(
        SeoTrackedKeyword entity,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/seo/internal/rank-tracking", entity, ct);
            if (!response.IsSuccessStatusCode)
                return Result<SeoTrackedKeyword>.Failure(
                    $"HTTP {(int)response.StatusCode}");

            var value = await response.Content.ReadFromJsonAsync<SeoTrackedKeyword>(cancellationToken: ct);
            return Result<SeoTrackedKeyword>.Success(value ?? entity);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to add tracked keyword");
            return Result<SeoTrackedKeyword>.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteKeywordAsync(
        Guid keywordId,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"api/seo/internal/rank-tracking/{keywordId:D}";
            var response = await _http.DeleteAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return Result.Failure($"HTTP {(int)response.StatusCode}");

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete tracked keyword {KeywordId}", keywordId);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<SeoRankTracking>>> GetHistoryAsync(
        Guid projectId,
        string keyword,
        int days,
        CancellationToken ct = default)
    {
        try
        {
            var url =
                $"api/seo/internal/rank-tracking/history?projectId={projectId:D}&keyword={Uri.EscapeDataString(keyword)}&days={days}";
            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyList<SeoRankTracking>>.Failure(
                    $"HTTP {(int)response.StatusCode}");

            var value = await response.Content.ReadFromJsonAsync<List<SeoRankTracking>>(cancellationToken: ct);
            return Result<IReadOnlyList<SeoRankTracking>>.Success(value ?? []);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to get rank history for project {ProjectId} keyword {Keyword}",
                projectId,
                keyword);
            return Result<IReadOnlyList<SeoRankTracking>>.Failure(ex.Message);
        }
    }

    public async Task<Result> UpsertSnapshotAsync(
        SeoRankTracking snapshot,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/seo/internal/rank-tracking/snapshot", snapshot, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Failed to upsert rank snapshot: HTTP {Status}", (int)response.StatusCode);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to upsert rank snapshot");
            return Result.Success();
        }
    }

    public async Task<Result<IReadOnlyList<Guid>>> ListProjectsWithKeywordsAsync(
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"api/seo/internal/rank-tracking/maintenance/projects?limit={limit}";
            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return Result<IReadOnlyList<Guid>>.Failure(
                    $"HTTP {(int)response.StatusCode}");

            var value = await response.Content.ReadFromJsonAsync<List<Guid>>(cancellationToken: ct);
            return Result<IReadOnlyList<Guid>>.Success(value ?? []);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to list projects with tracked keywords");
            return Result<IReadOnlyList<Guid>>.Failure(ex.Message);
        }
    }
}
