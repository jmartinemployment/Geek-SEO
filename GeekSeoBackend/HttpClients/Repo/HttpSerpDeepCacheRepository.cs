using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSerpDeepCacheRepository(IHttpClientFactory factory) : ISerpDeepCacheRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoSerpDeepCache?>> GetAsync(
        string keyword,
        string location,
        int resultCount,
        CancellationToken ct = default)
    {
        var url =
            $"api/seo/internal/serp-deep-cache?keyword={Uri.EscapeDataString(keyword)}&location={Uri.EscapeDataString(location)}&resultCount={resultCount}";
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoSerpDeepCache?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoSerpDeepCache>(ct);
        return Result<SeoSerpDeepCache?>.Success(value);
    }

    public async Task<Result<SeoSerpDeepCache>> UpsertAsync(SeoSerpDeepCache entry, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/seo/internal/serp-deep-cache", entry, ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoSerpDeepCache>.Failure(await response.Content.ReadAsStringAsync(ct));
        var value = await response.Content.ReadFromJsonAsync<SeoSerpDeepCache>(ct);
        return value is null
            ? Result<SeoSerpDeepCache>.Failure("Empty response")
            : Result<SeoSerpDeepCache>.Success(value);
    }
}
