using System.Net;
using System.Net.Http.Json;
using GeekApplication.Entities.Seo;
using GeekApplication.Results;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpGoogleIntegrationRepository(IHttpClientFactory factory) : IGoogleIntegrationRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoGscConnection?>> GetGscConnectionAsync(
        Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/google/gsc-connection?projectId={projectId}&userId={userId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoGscConnection?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoGscConnection?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var payload = await response.Content.ReadFromJsonAsync<SeoGscConnection>(ct);
        return Result<SeoGscConnection?>.Success(payload);
    }

    public async Task<Result<SeoGa4Connection?>> GetGa4ConnectionAsync(
        Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/seo/internal/google/ga4-connection?projectId={projectId}&userId={userId}",
            ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result<SeoGa4Connection?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoGa4Connection?>.Failure(await response.Content.ReadAsStringAsync(ct));
        var payload = await response.Content.ReadFromJsonAsync<SeoGa4Connection>(ct);
        return Result<SeoGa4Connection?>.Success(payload);
    }

    public async Task<Result<SeoGscConnection>> UpsertGscConnectionAsync(
        SeoGscConnection connection, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/google/gsc-connection?userId={connection.UserId}",
            connection,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoGscConnection>.Failure(await response.Content.ReadAsStringAsync(ct));
        var payload = await response.Content.ReadFromJsonAsync<SeoGscConnection>(ct);
        return payload is null
            ? Result<SeoGscConnection>.Failure("Empty response from internal Google GSC route.")
            : Result<SeoGscConnection>.Success(payload);
    }

    public async Task<Result<SeoGa4Connection>> UpsertGa4ConnectionAsync(
        SeoGa4Connection connection,
        Guid userId,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/google/ga4-connection?userId={userId}",
            connection,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoGa4Connection>.Failure(await response.Content.ReadAsStringAsync(ct));
        var payload = await response.Content.ReadFromJsonAsync<SeoGa4Connection>(ct);
        return payload is null
            ? Result<SeoGa4Connection>.Failure("Empty response from internal Google GA4 route.")
            : Result<SeoGa4Connection>.Success(payload);
    }

    public async Task<Result> DeleteConnectionsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync(
            $"api/seo/internal/google/connections?projectId={projectId}&userId={userId}",
            ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await response.Content.ReadAsStringAsync(ct));
    }
}
