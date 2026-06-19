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

public sealed class HttpSiteResearchRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : ISiteResearchRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public async Task<Result<SeoSiteResearch>> GetOrCreateForProjectAsync(
        Guid userId, CreateSiteResearchRequest request, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-research?userId={userId}", request, Json, ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<SeoSiteResearch>> GetWithPagesAsync(Guid siteResearchId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-research/{siteResearchId}?userId={user.UserId}", ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<SeoSiteResearch>> PersistStep1Async(
        Guid siteResearchId, SiteResearchStep1Write body, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-research/{siteResearchId}/step1?userId={user.UserId}", body, Json, ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<SeoSiteResearch>> ReplacePagesAsync(
        Guid siteResearchId, IReadOnlyList<SiteResearchPageWrite> pages, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-research/{siteResearchId}/pages?userId={user.UserId}", pages, Json, ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result<SeoSiteResearch>> PersistStep4Async(
        Guid siteResearchId, SiteResearchStep4Write body, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-research/{siteResearchId}/step4?userId={user.UserId}", body, Json, ct);
        return await ReadOneAsync(res, ct);
    }

    public async Task<Result> UpsertStepRunAsync(SiteAnalyzerStepRunUpsert upsert, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(
            $"api/seo/internal/site-analyzer/step-runs?userId={user.UserId}", upsert, Json, ct);
        if (!res.IsSuccessStatusCode)
            return Result.Failure(await res.Content.ReadAsStringAsync(ct));
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SiteAnalyzerStepRunRow>>> GetStepRunsForSiteAsync(
        Guid siteResearchId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analyzer/step-runs?siteResearchId={siteResearchId}&userId={user.UserId}", ct);
        return await ReadStepRunsAsync(res, ct);
    }

    public async Task<Result<IReadOnlyList<SiteAnalyzerStepRunRow>>> GetStepRunsForPackAsync(
        Guid urlResearchId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-analyzer/step-runs?urlResearchId={urlResearchId}&userId={user.UserId}", ct);
        return await ReadStepRunsAsync(res, ct);
    }

    private static async Task<Result<SeoSiteResearch>> ReadOneAsync(HttpResponseMessage res, CancellationToken ct)
    {
        if (res.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoSiteResearch>.NotFound("Site research not found");
        if (res.StatusCode == HttpStatusCode.Forbidden)
            return Result<SeoSiteResearch>.Failure("Access denied");
        if (!res.IsSuccessStatusCode)
            return Result<SeoSiteResearch>.Failure(await res.Content.ReadAsStringAsync(ct));
        var value = await res.Content.ReadFromJsonAsync<SeoSiteResearch>(Json, ct);
        return value is null
            ? Result<SeoSiteResearch>.Failure("Empty response")
            : Result<SeoSiteResearch>.Success(value);
    }

    private static async Task<Result<IReadOnlyList<SiteAnalyzerStepRunRow>>> ReadStepRunsAsync(
        HttpResponseMessage res, CancellationToken ct)
    {
        if (!res.IsSuccessStatusCode)
            return Result<IReadOnlyList<SiteAnalyzerStepRunRow>>.Failure(await res.Content.ReadAsStringAsync(ct));
        var list = await res.Content.ReadFromJsonAsync<List<SiteAnalyzerStepRunRow>>(Json, ct);
        return Result<IReadOnlyList<SiteAnalyzerStepRunRow>>.Success(list ?? []);
    }
}
