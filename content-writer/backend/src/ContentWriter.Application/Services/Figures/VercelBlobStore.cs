using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ContentWriter.Application.Services.Figures;

public sealed class VercelBlobStore(HttpClient http)
{
    private const string ApiBase = "https://blob.vercel-storage.com";
    private const string ApiVersion = "12";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<int> DeleteAllAsync(
        string token,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("BLOB_READ_WRITE_TOKEN is not configured.");
        }

        var deleted = 0;
        string? cursor = null;

        do
        {
            var list = await ListPageAsync(token, prefix, cursor, cancellationToken);
            if (list.Blobs.Count == 0)
            {
                break;
            }

            var urls = list.Blobs.Select(b => b.Url).ToList();
            await DeleteUrlsAsync(token, urls, cancellationToken);
            deleted += urls.Count;
            cursor = list.HasMore ? list.Cursor : null;
        }
        while (!string.IsNullOrWhiteSpace(cursor));

        return deleted;
    }

    private async Task<BlobListPage> ListPageAsync(
        string token,
        string? prefix,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var query = new List<string> { "limit=100" };
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            query.Add($"prefix={Uri.EscapeDataString(prefix)}");
        }

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{ApiBase}?{string.Join('&', query)}");
        AddAuthHeaders(request, token);

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blob list failed ({(int)response.StatusCode}): {body}");
        }

        var page = JsonSerializer.Deserialize<BlobListPage>(body, JsonOptions);
        return page ?? new BlobListPage();
    }

    private async Task DeleteUrlsAsync(
        string token,
        IReadOnlyList<string> urls,
        CancellationToken cancellationToken)
    {
        if (urls.Count == 0)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/delete");
        AddAuthHeaders(request, token);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { urls }),
            Encoding.UTF8,
            "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blob delete failed ({(int)response.StatusCode}): {body}");
        }
    }

    private static void AddAuthHeaders(HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("x-api-version", ApiVersion);
    }

    private sealed class BlobListPage
    {
        public List<BlobEntry> Blobs { get; set; } = [];
        public string? Cursor { get; set; }
        public bool HasMore { get; set; }
    }

    private sealed class BlobEntry
    {
        public string Url { get; set; } = string.Empty;
    }
}
