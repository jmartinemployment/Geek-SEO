using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Services;

public sealed class PayPalBillingService(
    PayPalOptions options,
    ISubscriptionService subscriptions,
    IHttpClientFactory httpClientFactory) : IPayPalBillingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public bool IsConfigured => options.IsConfigured && PayPalPlanMap.FromEnvironment() is not null;

    public PayPalCheckoutConfig? GetCheckoutConfig()
    {
        var map = PayPalPlanMap.FromEnvironment();
        if (!IsConfigured || map is null)
            return null;

        return new PayPalCheckoutConfig(options.ClientId, map.ToPublicMap());
    }

    public PayPalBillingStatus GetBillingStatus()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ClientId))
            missing.Add("PAYPAL_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            missing.Add("PAYPAL_CLIENT_SECRET");
        if (string.IsNullOrWhiteSpace(options.WebhookId))
            missing.Add("PAYPAL_WEBHOOK_ID");

        var map = PayPalPlanMap.FromEnvironment();
        if (map is null)
        {
            missing.Add("billing_plans_not_created");
        }

        var hasClient = !string.IsNullOrWhiteSpace(options.ClientId)
            && !string.IsNullOrWhiteSpace(options.ClientSecret);
        var hasWebhook = !string.IsNullOrWhiteSpace(options.WebhookId);
        var hasPlans = map is not null;

        return new PayPalBillingStatus(
            CheckoutAvailable: hasClient && hasWebhook && hasPlans,
            HasClientCredentials: hasClient,
            HasWebhookId: hasWebhook,
            HasAllPlanIds: hasPlans,
            MissingConfiguration: missing);
    }

    public async Task<Result> VerifyAndProcessWebhookAsync(
        IReadOnlyDictionary<string, string> headers,
        string rawBody,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            return Result.Failure("PayPal billing is not configured.");

        var verified = await VerifyWebhookSignatureAsync(headers, rawBody, ct);
        if (!verified)
            return Result.Failure("Invalid PayPal webhook signature.");

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventType = root.GetProperty("event_type").GetString() ?? string.Empty;
        if (!root.TryGetProperty("resource", out var resource))
            return Result.Success();

        var paypalSubscriptionId = ReadString(resource, "id");
        var planId = ReadString(resource, "plan_id");
        var customId = ReadString(resource, "custom_id");
        var status = ReadString(resource, "status") ?? "INACTIVE";
        var periodEnd = ReadDateTimeOffset(resource, "billing_info.next_billing_time")
            ?? ReadDateTimeOffset(resource, "billing_info.last_payment.time");

        if (!Guid.TryParse(customId, out var userId) || userId == Guid.Empty)
            return Result.Failure("PayPal webhook missing valid custom_id user id.");

        return eventType switch
        {
            "BILLING.SUBSCRIPTION.ACTIVATED" or "BILLING.SUBSCRIPTION.RE-ACTIVATED" =>
                await subscriptions.ApplyPayPalSubscriptionAsync(
                    userId,
                    paypalSubscriptionId ?? string.Empty,
                    planId ?? string.Empty,
                    status,
                    periodEnd,
                    ct),
            "BILLING.SUBSCRIPTION.CANCELLED"
                or "BILLING.SUBSCRIPTION.SUSPENDED"
                or "BILLING.SUBSCRIPTION.EXPIRED" =>
                await subscriptions.DeactivateSubscriptionAsync(userId, ct),
            _ => Result.Success(),
        };
    }

    public async Task<Result> CancelRemoteSubscriptionAsync(string paypalSubscriptionId, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return Result.Failure("PayPal billing is not configured.");
        if (string.IsNullOrWhiteSpace(paypalSubscriptionId))
            return Result.Failure("Missing PayPal subscription id.");

        var token = await GetAccessTokenAsync(ct);
        if (token is null)
            return Result.Failure("PayPal access token request failed.");

        var http = httpClientFactory.CreateClient("PayPal");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.ApiBase}/v1/billing/subscriptions/{paypalSubscriptionId}/cancel")
        {
            Content = new StringContent("{\"reason\":\"Customer requested cancellation\"}", Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure($"PayPal cancel failed ({(int)response.StatusCode}).");
    }

    private async Task<bool> VerifyWebhookSignatureAsync(
        IReadOnlyDictionary<string, string> headers,
        string rawBody,
        CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        if (token is null)
            return false;

        var payload = new
        {
            auth_algo = Header(headers, "PAYPAL-AUTH-ALGO"),
            cert_url = Header(headers, "PAYPAL-CERT-URL"),
            transmission_id = Header(headers, "PAYPAL-TRANSMISSION-ID"),
            transmission_sig = Header(headers, "PAYPAL-TRANSMISSION-SIG"),
            transmission_time = Header(headers, "PAYPAL-TRANSMISSION-TIME"),
            webhook_id = options.WebhookId,
            webhook_event = JsonSerializer.Deserialize<object>(rawBody, JsonOptions),
        };

        var http = httpClientFactory.CreateClient("PayPal");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.ApiBase}/v1/notifications/verify-webhook-signature")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return false;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var status = doc.RootElement.GetProperty("verification_status").GetString();
        return string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient("PayPal");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.ApiBase}/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
            }),
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.GetProperty("access_token").GetString();
    }

    private static string Header(IReadOnlyDictionary<string, string> headers, string name) =>
        headers.TryGetValue(name, out var value) ? value : string.Empty;

    private static string? ReadString(JsonElement element, string path)
    {
        var current = element;
        foreach (var segment in path.Split('.'))
        {
            if (!current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => current.ToString(),
        };
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string path)
    {
        var raw = ReadString(element, path);
        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }
}
