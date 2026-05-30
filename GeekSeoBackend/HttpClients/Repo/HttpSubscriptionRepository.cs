using GeekSeoBackend.Infrastructure;
using System.Net;
using System.Text.Json;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class HttpSubscriptionRepository(IHttpClientFactory factory) : ISubscriptionRepository
{
    private readonly HttpClient _http = factory.CreateClient(GeekDataGateway.HttpClientName);

    public async Task<Result<SeoSubscription?>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/seo/internal/subscriptions?userId={userId}", ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent)
            return Result<SeoSubscription?>.Success(null);
        if (!response.IsSuccessStatusCode)
            return Result<SeoSubscription?>.Failure(await response.Content.ReadAsStringAsync(ct));

        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
            return Result<SeoSubscription?>.Success(null);

        try
        {
            var value = JsonSerializer.Deserialize<SeoSubscription>(body);
            return Result<SeoSubscription?>.Success(value);
        }
        catch (JsonException)
        {
            return Result<SeoSubscription?>.Failure("Invalid subscription response from internal API.");
        }
    }

    public async Task<Result<SeoSubscription>> UpsertAsync(
        Guid userId,
        UpsertSubscriptionRequest request,
        CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/seo/internal/subscriptions?userId={userId}",
            request,
            ct);
        if (!response.IsSuccessStatusCode)
            return Result<SeoSubscription>.Failure(await response.Content.ReadAsStringAsync(ct));

        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
            return Result<SeoSubscription>.Failure("Empty subscription upsert response.");

        try
        {
            var value = JsonSerializer.Deserialize<SeoSubscription>(body);
            return value is null
                ? Result<SeoSubscription>.Failure("Empty subscription upsert response.")
                : Result<SeoSubscription>.Success(value);
        }
        catch (JsonException)
        {
            return Result<SeoSubscription>.Failure("Invalid subscription upsert response.");
        }
    }
}
