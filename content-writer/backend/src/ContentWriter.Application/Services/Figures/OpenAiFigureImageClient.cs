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

    public static readonly IReadOnlyList<string> AllowedModels =
    [
        "gpt-image-1",
        "dall-e-3",
        "dall-e-2",
    ];

    public async Task<byte[]> GeneratePngAsync(
        string prompt,
        int width,
        int height,
        string? modelOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_openAi.ApiKey))
        {
            throw new ContentGenerationException(
                "OpenAI API key is not configured (LlmProviders:OpenAi:ApiKey).");
        }

        var model = ResolveModel(modelOverride);
        var size = MapSize(model, width, height);
        // Do not send response_format — current Images API rejects it for several models.
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

    public static string ResolveModel(string? modelOverride, string? configuredDefault = null)
    {
        var candidate = string.IsNullOrWhiteSpace(modelOverride)
            ? (configuredDefault ?? "gpt-image-1")
            : modelOverride.Trim();

        if (!AllowedModels.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            throw new ContentGenerationException(
                $"Unsupported image model \"{candidate}\". Allowed: {string.Join(", ", AllowedModels)}.");
        }

        return AllowedModels.First(m => m.Equals(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveModel(string? modelOverride) =>
        ResolveModel(modelOverride, _imageOptions.OpenAiModel);

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

    private static string MapSize(string model, int width, int height)
    {
        var isDallE = model.StartsWith("dall-e", StringComparison.OrdinalIgnoreCase);

        if (isDallE)
        {
            // dall-e-3: 1024x1024, 1792x1024, 1024x1792
            // dall-e-2: 256/512/1024 square — use 1024x1024 for landscape requests
            if (model.Equals("dall-e-2", StringComparison.OrdinalIgnoreCase))
            {
                return "1024x1024";
            }

            if (width >= height * 1.2)
            {
                return "1792x1024";
            }

            if (height >= width * 1.2)
            {
                return "1024x1792";
            }

            return "1024x1024";
        }

        // gpt-image-1: 1024x1024, 1536x1024, 1024x1536
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
