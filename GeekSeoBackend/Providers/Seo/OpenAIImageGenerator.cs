using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

public sealed class OpenAIImageGenerator(IHttpClientFactory httpClientFactory) : IOpenAIImageGenerator
{
    private const string Model = "gpt-image-2";
    private const string DefaultSize = "1536x1024";

    public async Task<Result<FeaturedImageResult>> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result<FeaturedImageResult>.Failure("OPENAI_API_KEY is not configured on GeekSeoBackend.");

        var client = httpClientFactory.CreateClient("OpenAI");
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/images/generations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new OpenAIImageRequest
        {
            Model = Model,
            Prompt = prompt,
            Size = DefaultSize,
            Quality = "high",
            N = 1,
            ResponseFormat = "b64_json",
            OutputFormat = "webp",
        });

        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return Result<FeaturedImageResult>.Failure($"OpenAI image API failed ({(int)response.StatusCode}): {body}");

        var parsed = System.Text.Json.JsonSerializer.Deserialize<OpenAIImageResponse>(body);
        var b64 = parsed?.Data?.FirstOrDefault()?.B64Json;
        if (string.IsNullOrWhiteSpace(b64))
            return Result<FeaturedImageResult>.Failure("OpenAI image API returned no image data.");

        return Result<FeaturedImageResult>.Success(new FeaturedImageResult
        {
            DataUrl = $"data:image/webp;base64,{b64}",
            Prompt = prompt,
            MimeType = "image/webp",
        });
    }

    private sealed class OpenAIImageRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }

        [JsonPropertyName("size")]
        public required string Size { get; init; }

        [JsonPropertyName("quality")]
        public required string Quality { get; init; }

        [JsonPropertyName("n")]
        public required int N { get; init; }

        [JsonPropertyName("response_format")]
        public required string ResponseFormat { get; init; }

        [JsonPropertyName("output_format")]
        public required string OutputFormat { get; init; }
    }

    private sealed class OpenAIImageResponse
    {
        [JsonPropertyName("data")]
        public List<OpenAIImageData>? Data { get; init; }
    }

    private sealed class OpenAIImageData
    {
        [JsonPropertyName("b64_json")]
        public string? B64Json { get; init; }
    }
}
