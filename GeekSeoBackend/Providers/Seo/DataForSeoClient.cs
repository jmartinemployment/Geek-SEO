using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GeekSeoBackend.Providers.Seo;

internal static class DataForSeoClient
{
    internal static bool TryGetCredentials(out string login, out string password)
    {
        login = Environment.GetEnvironmentVariable("DATAFORSEO_LOGIN") ?? string.Empty;
        password = Environment.GetEnvironmentVariable("DATAFORSEO_PASSWORD") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(password);
    }

    internal static async Task<HttpResponseMessage> PostJsonAsync(
        IHttpClientFactory httpClientFactory,
        string path,
        object body,
        CancellationToken ct)
    {
        if (!TryGetCredentials(out var login, out var password))
        {
            throw new InvalidOperationException(
                "DATAFORSEO_LOGIN and DATAFORSEO_PASSWORD must be set. Sign up at https://dataforseo.com/");
        }

        var client = httpClientFactory.CreateClient("DataForSEO");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request, ct);
    }

    internal static bool IsApiSuccess(JsonElement root) =>
        !root.TryGetProperty("status_code", out var statusCode) || statusCode.GetInt32() == 20000;

    internal static string ReadApiErrorMessage(JsonElement root) =>
        root.TryGetProperty("status_message", out var sm) ? sm.GetString() ?? "DataForSEO error" : "DataForSEO error";
}
