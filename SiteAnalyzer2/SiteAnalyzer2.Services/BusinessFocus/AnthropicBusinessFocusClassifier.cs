using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SiteAnalyzer2.Services.BusinessFocus;

public class AnthropicBusinessFocusClassifier(HttpClient httpClient) : IBusinessFocusClassifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<BusinessFocusClassificationResult> ClassifyAsync(BusinessFocusInput input, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY is not configured.");

        var model = BusinessFocusProviderConfiguration.AnthropicModel;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.TryAddWithoutValidation("x-api-key", apiKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        request.Content = JsonContent.Create(new
        {
            model,
            max_tokens = 2048,
            system = BusinessFocusClassifierPrompt.SystemPrompt,
            messages = new object[]
            {
                new { role = "user", content = BusinessFocusClassifierPrompt.BuildUserPrompt(input) }
            }
        });

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Business focus Anthropic call failed: HTTP {(int)response.StatusCode} {body}");

        var completion = JsonSerializer.Deserialize<AnthropicMessagesResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Business focus Anthropic call returned an empty response.");

        var content = completion.Content.FirstOrDefault(block => block.Type == "text")?.Text
            ?? throw new InvalidOperationException("Business focus Anthropic call returned no text content.");

        return BusinessFocusResponseParser.Parse(content);
    }

    private sealed class AnthropicMessagesResponse
    {
        public List<ContentBlock> Content { get; set; } = [];
    }

    private sealed class ContentBlock
    {
        public string Type { get; set; } = string.Empty;
        public string? Text { get; set; }
    }
}
