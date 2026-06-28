using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>
/// Tries the primary provider (Claude); falls back to OpenAI on rate-limit (429) or quota error (400).
/// </summary>
public sealed class FallbackAIProvider(ClaudeProvider claude, OpenAIProvider openai) : IAIProvider
{
    public string ProviderName => "fallback";

    public async Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default)
    {
        var primary = await claude.CompleteAsync(request, ct);

        if (primary.IsSuccess)
            return primary;

        if (IsRateLimitOrQuotaError(primary.Error))
        {
            var fallback = await openai.CompleteAsync(request, ct);
            return fallback;
        }

        return primary;
    }

    private static bool IsRateLimitOrQuotaError(string? error) =>
        error is not null && (
            error.Contains("429", StringComparison.Ordinal) ||
            error.Contains("rate_limit", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("usage limit", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("API usage limits", StringComparison.OrdinalIgnoreCase));
}
