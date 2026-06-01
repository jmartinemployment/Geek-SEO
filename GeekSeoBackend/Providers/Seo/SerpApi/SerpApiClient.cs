using System.Net;
using System.Text.Json;
using GeekSeo.Application.Results;
using GeekSeoBackend.Extensions;

namespace GeekSeoBackend.Providers.Seo.SerpApi;

internal static class SerpApiClient
{
    internal static bool TryGetApiKey(out string apiKey)
    {
        apiKey = Environment.GetEnvironmentVariable(SeoProviderRegistration.SerpApiKeyEnv) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    internal static async Task<HttpResponseMessage> GetSearchAsync(
        IHttpClientFactory httpClientFactory,
        IReadOnlyDictionary<string, string> query,
        CancellationToken ct)
    {
        if (!TryGetApiKey(out var apiKey))
        {
            throw new InvalidOperationException(
                $"{SeoProviderRegistration.SerpApiKeyEnv} must be set. Sign up at https://serpapi.com/");
        }

        var pairs = query
            .Where(static kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(static kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")
            .ToList();
        pairs.Add($"api_key={Uri.EscapeDataString(apiKey)}");

        var path = $"search.json?{string.Join('&', pairs)}";
        var client = httpClientFactory.CreateClient("SerpApi");
        return await client.GetAsync(path, ct);
    }

    internal static bool IsSuccess(JsonElement root) =>
        root.TryGetProperty("search_metadata", out var meta)
        && meta.TryGetProperty("status", out var status)
        && string.Equals(status.GetString(), "Success", StringComparison.OrdinalIgnoreCase);

    internal static string ReadErrorMessage(JsonElement root)
    {
        if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            return error.GetString() ?? "SerpApi error";

        if (root.TryGetProperty("search_metadata", out var meta)
            && meta.TryGetProperty("status", out var status))
        {
            var statusText = status.GetString();
            if (!string.IsNullOrWhiteSpace(statusText))
                return $"SerpApi status: {statusText}";
        }

        return "SerpApi request failed";
    }

    internal static async Task<Result<string>> ReadBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            return Result<string>.Failure("SerpApi rate limit (429). Retry later.");

        if (!response.IsSuccessStatusCode)
            return Result<string>.Failure($"SerpApi HTTP {(int)response.StatusCode}: {Truncate(raw)}");

        return Result<string>.Success(raw);
    }

    internal static string Truncate(string raw) =>
        raw.Length <= 400 ? raw : raw[..400];

    internal static bool DomainMatchesUrl(string url, string targetDomain)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            host = host[4..];

        return host.Equals(targetDomain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + targetDomain, StringComparison.OrdinalIgnoreCase);
    }

    internal static string? HostFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            host = host[4..];

        return host;
    }
}
