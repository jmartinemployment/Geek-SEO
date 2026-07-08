using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContentImageSpike.Abstractions;
using ContentImageSpike.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentImageSpike.Infrastructure;

public sealed class LeonardoImageProvider : IImageGenerationProvider
{
    private const string BaseUrl = "https://cloud.leonardo.ai/api/rest/v1";

    private readonly HttpClient _http;
    private readonly ImageSpikeOptions _options;
    private readonly ILogger<LeonardoImageProvider> _logger;

    public LeonardoImageProvider(
        HttpClient http,
        IOptions<ImageSpikeOptions> options,
        ILogger<LeonardoImageProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => "leonardo";

    public async Task<ImageGenerationResult> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.LeonardoApiKey))
            throw new InvalidOperationException("LEONARDO_API_KEY (or ImageSpike:LeonardoApiKey) is not set.");

        var sw = Stopwatch.StartNew();
        var generationId = await StartGenerationAsync(request, cancellationToken);
        var imageUrl = await PollForImageUrlAsync(generationId, cancellationToken);

        using var imageResponse = await _http.GetAsync(imageUrl, cancellationToken);
        imageResponse.EnsureSuccessStatusCode();
        var bytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = imageResponse.Content.Headers.ContentType?.MediaType ?? "image/png";

        sw.Stop();
        _logger.LogInformation(
            "Leonardo generated {UseCase} ({Width}x{Height}) in {Ms}ms",
            request.UseCase,
            request.Width,
            request.Height,
            sw.ElapsedMilliseconds);

        return new ImageGenerationResult(ProviderId, request.UseCase, bytes, contentType, sw.Elapsed, imageUrl);
    }

    private async Task<string> StartGenerationAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new LeonardoCreateRequest
        {
            Prompt = request.Prompt,
            ModelId = _options.LeonardoModelId,
            Width = request.Width,
            Height = request.Height,
            NumImages = 1,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/generations");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.LeonardoApiKey);
        httpRequest.Content = JsonContent.Create(payload);

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Leonardo create generation failed ({(int)response.StatusCode}): {body}");

        var parsed = System.Text.Json.JsonSerializer.Deserialize<LeonardoCreateResponse>(body);
        var generationId = parsed?.SdGenerationJob?.GenerationId
            ?? throw new InvalidOperationException("Leonardo response missing generationId.");

        return generationId;
    }

    private async Task<string> PollForImageUrlAsync(string generationId, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.LeonardoPollTimeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/generations/{generationId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.LeonardoApiKey);

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Leonardo poll failed ({(int)response.StatusCode}): {body}");

            var parsed = System.Text.Json.JsonSerializer.Deserialize<LeonardoGetResponse>(body);
            var status = parsed?.GenerationsByPk?.Status ?? string.Empty;

            if (status.Equals("COMPLETE", StringComparison.OrdinalIgnoreCase))
            {
                var url = parsed?.GenerationsByPk?.GeneratedImages?.FirstOrDefault()?.Url;
                if (!string.IsNullOrWhiteSpace(url))
                    return url;

                throw new InvalidOperationException("Leonardo generation COMPLETE but no image URL returned.");
            }

            if (status.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Leonardo generation FAILED.");

            await Task.Delay(_options.LeonardoPollIntervalMs, cancellationToken);
        }

        throw new TimeoutException($"Leonardo generation {generationId} did not complete within {_options.LeonardoPollTimeoutSeconds}s.");
    }

    private sealed class LeonardoCreateRequest
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("modelId")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("num_images")]
        public int NumImages { get; set; }
    }

    private sealed class LeonardoCreateResponse
    {
        [JsonPropertyName("sdGenerationJob")]
        public LeonardoSdGenerationJob? SdGenerationJob { get; set; }
    }

    private sealed class LeonardoSdGenerationJob
    {
        [JsonPropertyName("generationId")]
        public string? GenerationId { get; set; }
    }

    private sealed class LeonardoGetResponse
    {
        [JsonPropertyName("generations_by_pk")]
        public LeonardoGeneration? GenerationsByPk { get; set; }
    }

    private sealed class LeonardoGeneration
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("generated_images")]
        public List<LeonardoGeneratedImage>? GeneratedImages { get; set; }
    }

    private sealed class LeonardoGeneratedImage
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
