using System.Net.Http.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Infrastructure;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpKeywordVendorSnapshotRepository(IHttpClientFactory factory) : IKeywordVendorSnapshotRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoKeywordVendorSnapshot?>> GetAsync(
        string seedKeyword,
        string location,
        string languageCode,
        CancellationToken ct = default)
    {
        var url =
            $"api/seo/internal/keyword-vendor-snapshots?seedKeyword={Uri.EscapeDataString(seedKeyword)}&location={Uri.EscapeDataString(location)}&languageCode={Uri.EscapeDataString(languageCode)}";
        var response = await _http.GetAsync(url, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result<SeoKeywordVendorSnapshot?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoKeywordVendorSnapshot?>.Failure(await response.Content.ReadAsStringAsync(ct));

        var row = await response.Content.ReadFromJsonAsync<SeoKeywordVendorSnapshot>(cancellationToken: ct);
        return Result<SeoKeywordVendorSnapshot?>.Success(row);
    }

    public async Task<Result<SeoKeywordVendorSnapshot>> UpsertAsync(
        SeoKeywordVendorSnapshot entry,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync("api/seo/internal/keyword-vendor-snapshots", entry, ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoKeywordVendorSnapshot>.Failure(await response.Content.ReadAsStringAsync(ct));

        var row = await response.Content.ReadFromJsonAsync<SeoKeywordVendorSnapshot>(cancellationToken: ct);
        return row is null
            ? Result<SeoKeywordVendorSnapshot>.Success(entry)
            : Result<SeoKeywordVendorSnapshot>.Success(row);
    }
}
