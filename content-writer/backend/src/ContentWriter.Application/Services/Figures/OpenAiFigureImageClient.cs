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
        var payload = new
        {
            model = _imageOptions.OpenAiModel,
            prompt,
            n = 1,
            size,
            response_format = "b64_json",
        };

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

        using var document = JsonDocument.Parse(body);
        var b64 = document.RootElement
            .GetProperty("data")[0]
            .GetProperty("b64_json")
            .GetString();

        if (string.IsNullOrWhiteSpace(b64))
        {
            throw new ContentGenerationException("OpenAI image response missing image data.");
        }

        return Convert.FromBase64String(b64);
    }

    private static string MapSize(int width, int height) => (width, height) switch
    {
        (>= 1792, >= 1024) => "1792x1024",
        (>= 1024, >= 1792) => "1024x1792",
        _ => "1024x1024",
    };
}
