using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentWriter.Application.Providers;
using Microsoft.Extensions.Options;

namespace ContentWriter.Application.Services.Figures;

public sealed class OpenAiFigureImageClient(
    HttpClient http,
    IOptions<LlmProvidersOptions> llmOptions,
    IOptions<FigureImageGenerationOptions> imageOptions)
{
    private readonly OpenAiOptions _openAi = llmOptions.Value.OpenAi;
    private readonly FigureImageGenerationOptions _imageOptions = imageOptions.Value;

    public async Task<byte[]> GeneratePngAsync(
        string prompt,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_openAi.ApiKey))
        {
            throw new ContentGenerationException(
                "OpenAI API key is not configured (LlmProviders:OpenAi:ApiKey).");
        }

        var size = MapSize(width, height);
        var model = _imageOptions.OpenAiModel;
        // Do not send response_format — gpt-image-* and current Images API reject it as unknown.
        var payload = new { model, prompt, n = 1, size };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAi.ApiKey);
        request.Content = JsonContent.Create(payload);

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ContentGenerationException(
                $"OpenAI image generation failed ({(int)response.StatusCode}): {body}");
        }

        return await ExtractImageBytesAsync(body, cancellationToken);
    }

    private async Task<byte[]> ExtractImageBytesAsync(string body, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(body);
        var item = document.RootElement.GetProperty("data")[0];

        if (item.TryGetProperty("b64_json", out var b64Prop))
        {
            var b64 = b64Prop.GetString();
            if (!string.IsNullOrWhiteSpace(b64))
            {
                return Convert.FromBase64String(b64);
            }
        }

        if (item.TryGetProperty("url", out var urlProp))
        {
            var url = urlProp.GetString();
            if (!string.IsNullOrWhiteSpace(url))
            {
                return await http.GetByteArrayAsync(url, cancellationToken);
            }
        }

        throw new ContentGenerationException("OpenAI image response missing image data.");
    }

    private static string MapSize(int width, int height)
    {
        // gpt-image-1 sizes: 1024x1024, 1536x1024, 1024x1536 (not dall-e 1792x1024).
        if (width >= height * 1.2)
        {
            return "1536x1024";
        }

        if (height >= width * 1.2)
        {
            return "1024x1536";
        }

        return "1024x1024";
    }
}
