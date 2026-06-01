using System.Net;
using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSerpDeepCacheRepository(
    IHttpClientFactory factory,
    ILogger<HttpSerpDeepCacheRepository> logger) : ISerpDeepCacheRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoSerpDeepCache?>> GetAsync(
        string keyword,
        string location,
        int resultCount,
        CancellationToken ct = default)
    {
        try
        {
            var url =
                $"api/seo/internal/serp-deep-cache?keyword={Uri.EscapeDataString(keyword)}&location={Uri.EscapeDataString(location)}&resultCount={resultCount}";
            var response = await _http.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return Result<SeoSerpDeepCache?>.Success(null);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "SERP deep cache read failed ({Status}): {Body}",
                    (int)response.StatusCode,
                    Truncate(body));
                return Result<SeoSerpDeepCache?>.Success(null);
            }

            var value = await response.Content.ReadFromJsonAsync<SeoSerpDeepCache>(cancellationToken: ct);
            return Result<SeoSerpDeepCache?>.Success(value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SERP deep cache read skipped");
            return Result<SeoSerpDeepCache?>.Success(null);
        }
    }

    public async Task<Result<SeoSerpDeepCache>> UpsertAsync(SeoSerpDeepCache entry, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PutAsJsonAsync("api/seo/internal/serp-deep-cache", entry, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "SERP deep cache write failed ({Status}): {Body}",
                    (int)response.StatusCode,
                    Truncate(body));
                return Result<SeoSerpDeepCache>.Success(entry);
            }

            var value = await response.Content.ReadFromJsonAsync<SeoSerpDeepCache>(cancellationToken: ct);
            return value is null
                ? Result<SeoSerpDeepCache>.Success(entry)
                : Result<SeoSerpDeepCache>.Success(value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SERP deep cache write skipped");
            return Result<SeoSerpDeepCache>.Success(entry);
        }
    }

    private static string Truncate(string raw) =>
        raw.Length <= 300 ? raw : raw[..300];
}
