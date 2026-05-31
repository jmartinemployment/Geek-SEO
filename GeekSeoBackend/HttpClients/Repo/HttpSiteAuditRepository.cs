using System.Net;
using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSiteAuditRepository(IHttpClientFactory factory, ICurrentUserContext user) : ISiteAuditRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoSiteAudit>> CreateAsync(SeoSiteAudit audit, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-audits?userId={user.UserId}",
            new CreateSiteAuditRequest(audit.ProjectId),
            ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<SeoSiteAudit>> GetByIdAsync(Guid auditId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/site-audits/{auditId}?userId={user.UserId}", ct);
        return await ReadOneAsync(response, ct);
    }

    public async Task<Result<IReadOnlyList<SeoSiteAudit>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/site-audits?projectId={projectId}&userId={user.UserId}",
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<SeoSiteAudit>>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<List<SeoSiteAudit>>(ct);
        return Result<IReadOnlyList<SeoSiteAudit>>.Success(value ?? []);
    }

    public async Task<Result> UpdateStatusAsync(Guid auditId, UpdateSiteAuditStatusRequest request, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/seo/internal/site-audits/{auditId}/status?userId={user.UserId}",
            request,
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    public async Task<Result> AppendPagesAsync(Guid auditId, AppendSiteAuditPagesRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/seo/internal/site-audits/{auditId}/pages?userId={user.UserId}",
            request,
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<Result<SeoSiteAudit>> ReadOneAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoSiteAudit>.NotFound("Not found");
        if (!response.IsSuccessStatusCode)
            return Result<SeoSiteAudit>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoSiteAudit>(ct);
        return value is null ? Result<SeoSiteAudit>.Failure("Empty response") : Result<SeoSiteAudit>.Success(value);
    }
}
