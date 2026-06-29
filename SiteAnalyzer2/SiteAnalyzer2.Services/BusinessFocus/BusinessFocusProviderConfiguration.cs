namespace SiteAnalyzer2.Services.BusinessFocus;

public enum BusinessFocusProvider
{
    OpenAi,
    Anthropic,
    Human
}

public static class BusinessFocusProviderConfiguration
{
    public const string DefaultOpenAiModel = "gpt-4o-mini";
    public const string DefaultAnthropicModel = "claude-haiku-4-5-20251001";

    public static string OpenAiModel =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_MODEL"))
            ? DefaultOpenAiModel
            : Environment.GetEnvironmentVariable("OPENAI_MODEL")!.Trim();

    public static string AnthropicModel =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_MODEL"))
            ? DefaultAnthropicModel
            : Environment.GetEnvironmentVariable("ANTHROPIC_MODEL")!.Trim();

    public static BusinessFocusProvider ResolveEffectiveProvider()
    {
        var setting = ReadProviderSetting();
        if (string.IsNullOrWhiteSpace(setting))
        {
            throw new InvalidOperationException(
                "BUSINESS_FOCUS_PROVIDER is required. Set to openai, anthropic, or human.");
        }

        return setting.ToLowerInvariant() switch
        {
            "openai" => RequireOpenAi(),
            "anthropic" => RequireAnthropic(),
            "human" => BusinessFocusProvider.Human,
            "auto" => throw new InvalidOperationException(
                "BUSINESS_FOCUS_PROVIDER=auto is no longer supported. Set openai, anthropic, or human explicitly."),
            _ => throw new InvalidOperationException(
                $"BUSINESS_FOCUS_PROVIDER={setting} is invalid. Set openai, anthropic, or human.")
        };
    }

    public static string ResolveProviderName() =>
        ToProviderName(ResolveEffectiveProvider());

    public static string ToProviderName(BusinessFocusProvider provider) =>
        provider switch
        {
            BusinessFocusProvider.OpenAi => "openai",
            BusinessFocusProvider.Anthropic => "anthropic",
            BusinessFocusProvider.Human => "human",
            _ => throw new InvalidOperationException($"Unknown business focus provider: {provider}")
        };

    public static bool UsesAutomaticClassification(BusinessFocusProvider provider) =>
        provider is BusinessFocusProvider.OpenAi or BusinessFocusProvider.Anthropic;

    private static string? ReadProviderSetting() =>
        Environment.GetEnvironmentVariable("BUSINESS_FOCUS_PROVIDER")
        ?? Environment.GetEnvironmentVariable("BUSINESS_FOCUS_AI_PROVIDER");

    private static BusinessFocusProvider RequireOpenAi()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            throw new InvalidOperationException(
                "BUSINESS_FOCUS_PROVIDER=openai but OPENAI_API_KEY is not set.");
        }

        return BusinessFocusProvider.OpenAi;
    }

    private static BusinessFocusProvider RequireAnthropic()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
        {
            throw new InvalidOperationException(
                "BUSINESS_FOCUS_PROVIDER=anthropic but ANTHROPIC_API_KEY is not set.");
        }

        return BusinessFocusProvider.Anthropic;
    }
}
