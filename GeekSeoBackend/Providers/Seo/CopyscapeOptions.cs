namespace GeekSeoBackend.Providers.Seo;

public sealed class CopyscapeOptions
{
    public string Username { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public decimal SpendLimitUsd { get; init; } = 0.50m;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(ApiKey);
}
