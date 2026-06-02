using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpNicheAnalyticsDapperRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : INicheAnalyticsDapperRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<NicheProfileSummary?>> GetProfileSummaryAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-analytics/{profileId}/summary?userId={user.UserId}", ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<NicheProfileSummary?>.Success(null);
        if (!res.IsSuccessStatusCode)
            return Result<NicheProfileSummary?>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<NicheProfileSummary?>(Json, ct);
        return Result<NicheProfileSummary?>.Success(value);
    }

    public async Task<Result<IReadOnlyList<PillarCoverageMatrix>>> GetCoverageMatrixAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-analytics/{profileId}/coverage-matrix?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<PillarCoverageMatrix>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<PillarCoverageMatrix>>(Json, ct);
        return Result<IReadOnlyList<PillarCoverageMatrix>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<TopicalGapSummary>>> GetTopicalGapsAsync(
        Guid profileId, bool quickWinsOnly = false, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-analytics/{profileId}/gaps?quickWinsOnly={quickWinsOnly}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<TopicalGapSummary>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<TopicalGapSummary>>(Json, ct);
        return Result<IReadOnlyList<TopicalGapSummary>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<AuthorityProgressPoint>>> GetAuthorityProgressAsync(
        Guid projectId, int months = 12, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-analytics/project/{projectId}/progress?months={months}&userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<AuthorityProgressPoint>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<AuthorityProgressPoint>>(Json, ct);
        return Result<IReadOnlyList<AuthorityProgressPoint>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<CompetitorNicheOverlap>>> GetCompetitorOverlapAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-analytics/{profileId}/competitors?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<CompetitorNicheOverlap>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<CompetitorNicheOverlap>>(Json, ct);
        return Result<IReadOnlyList<CompetitorNicheOverlap>>.Success(value ?? []);
    }

    public async Task<Result<IReadOnlyList<EntityCoverageReport>>> GetEntityCoverageAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/niche-analytics/{profileId}/entities?userId={user.UserId}", ct);
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<EntityCoverageReport>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<List<EntityCoverageReport>>(Json, ct);
        return Result<IReadOnlyList<EntityCoverageReport>>.Success(value ?? []);
    }
}
