using System.Net.Http.Json;
using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

public sealed class OpenAIProvider(IHttpClientFactory httpClientFactory) : IAIProvider
{
    public string ProviderName => "openai";

    public async Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result<AIResponse>.Failure("OPENAI_API_KEY is not set on GeekSeoBackend.");

        var client = httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var model = ResolveModel(request.Model);

        var body = new
        {
            model,
            max_completion_tokens = request.MaxTokens,
            temperature = request.Temperature,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt },
            },
        };

        using var response = await client.PostAsJsonAsync("/v1/chat/completions", body, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return Result<AIResponse>.Failure($"OpenAI API {(int)response.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var content = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
        var responseModel = root.GetProperty("model").GetString() ?? model;
        var usage = root.GetProperty("usage");
        var inputTokens = usage.GetProperty("prompt_tokens").GetInt32();
        var outputTokens = usage.GetProperty("completion_tokens").GetInt32();
        var stop = root.GetProperty("choices")[0].GetProperty("finish_reason").GetString() ?? "stop";

        return Result<AIResponse>.Success(new AIResponse
        {
            Content = content,
            Model = responseModel,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            StopReason = stop,
        });
    }

    internal static string ResolveModel(string? requestedModel)
    {
        var envModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        if (!string.IsNullOrWhiteSpace(envModel))
            return envModel.Trim();

        return "gpt-4o";
    }
}
