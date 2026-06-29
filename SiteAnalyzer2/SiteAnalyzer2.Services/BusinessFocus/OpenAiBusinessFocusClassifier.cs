using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SiteAnalyzer2.Services.BusinessFocus;

public class OpenAiBusinessFocusClassifier(HttpClient httpClient) : IBusinessFocusClassifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<BusinessFocusClassificationResult> ClassifyAsync(BusinessFocusInput input, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY is not configured.");

        var model = BusinessFocusProviderConfiguration.OpenAiModel;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = BusinessFocusClassifierPrompt.SystemPrompt },
                new { role = "user", content = BusinessFocusClassifierPrompt.BuildUserPrompt(input) }
            }
        });

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Business focus OpenAI call failed: HTTP {(int)response.StatusCode} {body}");

        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Business focus OpenAI call returned an empty response.");

        var content = completion.Choices.FirstOrDefault()?.Message.Content
            ?? throw new InvalidOperationException("Business focus OpenAI call returned no message content.");

        return BusinessFocusResponseParser.Parse(content);
    }

    private sealed class ChatCompletionResponse
    {
        public List<Choice> Choices { get; set; } = [];
    }

    private sealed class Choice
    {
        public Message Message { get; set; } = new();
    }

    private sealed class Message
    {
        public string? Content { get; set; }
    }
}
