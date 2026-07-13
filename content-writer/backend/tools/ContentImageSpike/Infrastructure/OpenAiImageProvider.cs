using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ContentImageSpike.Abstractions;
using ContentImageSpike.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentImageSpike.Infrastructure;

public sealed class OpenAiImageProvider : IImageGenerationProvider
{
    private readonly HttpClient _http;
    private readonly ImageSpikeOptions _options;
    private readonly ILogger<OpenAiImageProvider> _logger;

    public OpenAiImageProvider(
        HttpClient http,
        IOptions<ImageSpikeOptions> options,
        ILogger<OpenAiImageProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => "openai";

    public async Task<ImageGenerationResult> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.OpenAiApiKey))
            throw new InvalidOperationException("OPENAI_API_KEY (or ImageSpike:OpenAiApiKey) is not set.");

        var size = MapSize(request.Width, request.Height);
        var payload = new
        {
            model = _options.OpenAiModel,
            prompt = request.Prompt,
            n = 1,
            size,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAiApiKey);
        httpRequest.Content = JsonContent.Create(payload);

        var sw = Stopwatch.StartNew();
        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI images API failed ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var item = doc.RootElement.GetProperty("data")[0];
        byte[] bytes;
        if (item.TryGetProperty("b64_json", out var b64Prop) && b64Prop.GetString() is { Length: > 0 } b64)
        {
            bytes = Convert.FromBase64String(b64);
        }
        else if (item.TryGetProperty("url", out var urlProp) && urlProp.GetString() is { Length: > 0 } url)
        {
            bytes = await _http.GetByteArrayAsync(url, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("OpenAI response missing image data.");
        }

        sw.Stop();

        _logger.LogInformation(
            "OpenAI generated {UseCase} ({Width}x{Height}) in {Ms}ms",
            request.UseCase,
            request.Width,
            request.Height,
            sw.ElapsedMilliseconds);

        return new ImageGenerationResult(ProviderId, request.UseCase, bytes, "image/png", sw.Elapsed);
    }

    private static string MapSize(int width, int height)
    {
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
