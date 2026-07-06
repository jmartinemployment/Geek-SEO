namespace GeekSeo.Application.Models.Seo;

public static class AnthropicModels
{
    /// <summary>Cheapest current Claude — default while testing content writing.</summary>
    public const string DefaultHaiku = "claude-haiku-4-5-20251001";

    /// <summary>Higher quality Sonnet — set ANTHROPIC_MODEL to this for production writing.</summary>
    public const string DefaultSonnet = "claude-sonnet-4-6";

    /// <summary>Active default when ANTHROPIC_MODEL is unset. Retired 2026-06-15: claude-sonnet-4-20250514.</summary>
    public const string Default = DefaultHaiku;
}

public sealed record AIRequest
{
    public required string SystemPrompt { get; init; }
    public required string UserPrompt { get; init; }
    public string Model { get; init; } = AnthropicModels.Default;
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.7;
}

public sealed record AIResponse
{
    public required string Content { get; init; }
    public required string Model { get; init; }
    public required int InputTokens { get; init; }
    public required int OutputTokens { get; init; }
    public required string StopReason { get; init; }
}
