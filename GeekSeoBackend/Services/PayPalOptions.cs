namespace GeekSeoBackend.Services;

public sealed record PayPalOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string WebhookId { get; init; }
    public bool UseSandbox { get; init; }

    public string ApiBase => UseSandbox
        ? "https://api-m.sandbox.paypal.com"
        : "https://api-m.paypal.com";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(WebhookId);
}
