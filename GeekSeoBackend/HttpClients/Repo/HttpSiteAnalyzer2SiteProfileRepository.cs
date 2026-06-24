using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSiteAnalyzer2SiteProfileRepository(
    IHttpClientFactory factory,
    ICurrentUserContext user) : ISiteAnalyzer2SiteProfileRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<SiteAnalyzer2SiteProfileExport>> GetByIdAsync(
        Guid siteProfileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-profiles?site_profile={siteProfileId}&userId={user.UserId}",
            ct);

        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<SiteAnalyzer2SiteProfileExport>.NotFound("Site profile not found");

        if (!res.IsSuccessStatusCode)
            return Result<SiteAnalyzer2SiteProfileExport>.Failure(await res.Content.ReadAsStringAsync(ct));

        var value = await res.Content.ReadFromJsonAsync<SiteAnalyzer2SiteProfileExport>(Json, ct);
        return value is null
            ? Result<SiteAnalyzer2SiteProfileExport>.Failure("Empty site profile response")
            : Result<SiteAnalyzer2SiteProfileExport>.Success(value);
    }

    public async Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleAsync(
        Guid siteProfileId, CancellationToken ct = default)
    {
        var res = await _http.GetAsync(
            $"api/seo/internal/site-profiles/{siteProfileId}/content-writer-bundle?userId={user.UserId}",
            ct);

        if (res.StatusCode is HttpStatusCode.NotFound)
            return Result<ContentWriterSiteBundle>.NotFound("Site profile not found");

        if (!res.IsSuccessStatusCode)
            return Result<ContentWriterSiteBundle>.Failure(await res.Content.ReadAsStringAsync(ct));

        var value = await res.Content.ReadFromJsonAsync<ContentWriterSiteBundle>(Json, ct);
        return value is null
            ? Result<ContentWriterSiteBundle>.Failure("Empty site bundle response")
            : Result<ContentWriterSiteBundle>.Success(value);
    }
}
