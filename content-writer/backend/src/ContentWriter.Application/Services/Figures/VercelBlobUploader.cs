using System.Net.Http.Headers;
using System.Text.Json;

namespace ContentWriter.Application.Services.Figures;

public sealed class VercelBlobUploader(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<string> UploadPublicAsync(
        string pathname,
        byte[] content,
        string contentType,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("BLOB_READ_WRITE_TOKEN is not configured.");
        }

        var encodedPath = string.Join(
            '/',
            pathname.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"https://blob.vercel-storage.com/{encodedPath}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("x-vercel-blob-access", "public");
        request.Headers.Add("x-add-random-suffix", "0");
        request.Headers.Add("x-allow-overwrite", "1");
        request.Content = new ByteArrayContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Blob upload failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        var url = document.RootElement.GetProperty("url").GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Blob upload response did not include a url.");
        }

        return url;
    }
}
