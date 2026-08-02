using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSiteAnalysisAnalyticsRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : ISiteAnalysisAnalyticsRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<SiteAnalysisProfileSummary?>> GetProfileSummaryAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-analytics/{profileId}/summary?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<SiteAnalysisProfileSummary?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalysisProfileSummary?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<SiteAnalysisProfileSummary?>(Json, ct);
        return Result<SiteAnalysisProfileSummary?>.Success(value);
    }

    public async Task<Result<IReadOnlyList<PillarCoverageMatrix>>> GetCoverageMatrixAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-analytics/{profileId}/coverage-matrix?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<PillarCoverageMatrix>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<PillarCoverageMatrix>>(Json, ct);
        return Result<IReadOnlyList<PillarCoverageMatrix>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<TopicalGapSummary>>> GetTopicalGapsAsync(
        Guid profileId, bool quickWinsOnly = false, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-analytics/{profileId}/gaps?quickWinsOnly={quickWinsOnly}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<TopicalGapSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<TopicalGapSummary>>(Json, ct);
        return Result<IReadOnlyList<TopicalGapSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<AuthorityProgressPoint>>> GetAuthorityProgressAsync(
        Guid projectId, int months = 12, CancellationToken ct = default)
    {
        try
        {
            if (!user.IsAuthenticated)
                return Result<IReadOnlyList<AuthorityProgressPoint>>.Success([]);

            var res = await _http.GetAsync(
                $"api/seo/internal/site-analysis-profiles/project/{projectId}/progress?months={months}&userId={user.UserId}", ct);
            if (!res.IsSuccessStatusCode)
                return Result<IReadOnlyList<AuthorityProgressPoint>>.Success([]);

            var value = await res.Content.ReadFromJsonAsync<List<AuthorityProgressPoint>>(Json, ct);
            return Result<IReadOnlyList<AuthorityProgressPoint>>.Success(value ?? []);
        }
        catch (Exception)
        {
            return Result<IReadOnlyList<AuthorityProgressPoint>>.Success([]);
        }
    }

    public async Task<Result<IReadOnlyList<CompetitorFocusOverlap>>> GetCompetitorOverlapAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-analytics/{profileId}/competitors?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<CompetitorFocusOverlap>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<CompetitorFocusOverlap>>(Json, ct);
        return Result<IReadOnlyList<CompetitorFocusOverlap>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<EntityCoverageReport>>> GetEntityCoverageAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analysis-analytics/{profileId}/entities?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<EntityCoverageReport>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<EntityCoverageReport>>(Json, ct);
        return Result<IReadOnlyList<EntityCoverageReport>>.Success(value ?? []);
    }
}
