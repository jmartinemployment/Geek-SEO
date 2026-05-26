using System.Net.Http.Json;
using GeekApplication.Entities.Seo;
using GeekApplication.Interfaces.Seo;
using GeekApplication.Models.Seo;
using GeekApplication.Results;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSerpCacheRepository(IHttpClientFactory factory) : ISerpCacheRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoSerpResult?>> GetAsync(
        string keyword, string location, string languageCode, CancellationToken ct = default)
    {
        var url =
            $"api/seo/internal/serp-cache?keyword={Uri.EscapeDataString(keyword)}&location={Uri.EscapeDataString(location)}&languageCode={languageCode}";
        var response = await _http.GetAsync(url, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result<SeoSerpResult?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoSerpResult?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var row = await response.Content.ReadFromJsonAsync<SeoSerpResult>(ct);
        return Result<SeoSerpResult?>.Success(row);
    }

    public async Task<Result<SeoSerpResult>> UpsertAsync(
        string keyword, string location, string languageCode,
        SerpResult serp, SerpBenchmarksPayload benchmarks,
        CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/seo/internal/serp-cache",
            new { keyword, location, languageCode, serp, benchmarks },
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoSerpResult>.Failure(await response.Content.ReadAsStringAsync(ct));
        var row = await response.Content.ReadFromJsonAsync<SeoSerpResult>(ct);
        return row is null
            ? Result<SeoSerpResult>.Failure("Empty SERP cache response")
            : Result<SeoSerpResult>.Success(row);
    }

    public async Task<Result> DeleteAsync(
        string keyword, string location, string languageCode, CancellationToken ct = default)
    {
        var url =
            $"api/seo/internal/serp-cache?keyword={Uri.EscapeDataString(keyword)}&location={Uri.EscapeDataString(location)}&languageCode={languageCode}";
        var response = await _http.DeleteAsync(url, ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }
}
