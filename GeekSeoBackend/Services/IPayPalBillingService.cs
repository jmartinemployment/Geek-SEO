using GeekSeo.Application.Results;

namespace GeekSeoBackend.Services;

public interface IPayPalBillingService
{
    bool IsConfigured { get; }
    PayPalCheckoutConfig? GetCheckoutConfig();
    PayPalBillingStatus GetBillingStatus();
    Task<Result> VerifyAndProcessWebhookAsync(
        IReadOnlyDictionary<string, string> headers,
        string rawBody,
        CancellationToken ct = default);
    Task<Result> CancelRemoteSubscriptionAsync(string paypalSubscriptionId, CancellationToken ct = default);
}

public sealed record PayPalCheckoutConfig(string ClientId, IReadOnlyDictionary<string, string> PlanIds);

public sealed record PayPalBillingStatus(
    bool CheckoutAvailable,
    bool HasClientCredentials,
    bool HasWebhookId,
    bool HasAllPlanIds,
    IReadOnlyList<string> MissingConfiguration);
