using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Extensions;
using GeekSeoBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeekSeoBackend.Controllers.Seo;

[ApiController]
[Route("api/seo/subscription")]
public sealed class SubscriptionController(
    ISubscriptionService subscriptions,
    IPayPalBillingService paypal,
    ICurrentUserContext user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        try
        {
            var result = await subscriptions.GetSummaryAsync(user.RequireUserId(), ct);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            return Ok(result.Value);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required" });
        }
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult Plans()
    {
        var billing = paypal.GetBillingStatus();
        var checkout = paypal.GetCheckoutConfig();

        return Ok(new
        {
            tiers = SubscriptionCatalog.Tiers.Select(t => new
            {
                key = t.Key,
                name = t.Name,
                priceLabel = t.PriceLabel,
                priceMonthly = t.PriceMonthly,
                highlights = t.Highlights,
            }),
            checkout = new
            {
                available = billing.CheckoutAvailable,
                provider = "paypal",
                deferred = !billing.CheckoutAvailable,
                clientId = checkout?.ClientId,
                planIds = checkout?.PlanIds,
                missing = billing.MissingConfiguration,
                plansSetupHint =
                    "PayPal does not provide PAYPAL_PLAN_* variables. Run: node scripts/paypal-create-subscription-plans.mjs",
            },
            manualTierChangeEnabled = ManualTierChangeEnabled(),
        });
    }

    /// <summary>
    /// Dev/staging only: set tier without PayPal. Enable with SUBSCRIPTION_MANUAL_TIER_ENABLED=true.
    /// </summary>
    [HttpPost("tier")]
    public async Task<IActionResult> SetTier([FromBody] SetTierRequest request, CancellationToken ct)
    {
        if (!ManualTierChangeEnabled())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Tier))
            return BadRequest(new { error = "tier is required" });

        try
        {
            var result = await subscriptions.SetTierManuallyAsync(user.RequireUserId(), request.Tier, ct);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.Error });

            var summary = await subscriptions.GetSummaryAsync(user.RequireUserId(), ct);
            return summary.IsSuccess
                ? Ok(summary.Value)
                : Ok(new { tier = request.Tier.Trim().ToLowerInvariant(), status = "active" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required" });
        }
    }

    [HttpPost("webhooks/paypal")]
    [AllowAnonymous]
    public async Task<IActionResult> PayPalWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(rawBody))
            return BadRequest(new { error = "Empty webhook body." });

        var headers = Request.Headers
            .Where(h => h.Key.StartsWith("PAYPAL-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var result = await paypal.VerifyAndProcessWebhookAsync(headers, rawBody, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { received = true });
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken ct)
    {
        try
        {
            var userId = user.RequireUserId();
            var summary = await subscriptions.GetSummaryAsync(userId, ct);
            if (!summary.IsSuccess)
                return BadRequest(new { error = summary.Error });

            if (!string.IsNullOrWhiteSpace(summary.Value?.PaypalSubscriptionId))
            {
                var remote = await paypal.CancelRemoteSubscriptionAsync(summary.Value.PaypalSubscriptionId, ct);
                if (!remote.IsSuccess)
                    return BadRequest(new { error = remote.Error });
            }

            var cancelled = await subscriptions.CancelPayPalSubscriptionAsync(userId, ct);
            return cancelled.IsSuccess
                ? Ok(new { cancelled = true })
                : BadRequest(new { error = cancelled.Error });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Authentication required" });
        }
    }

    private static bool ManualTierChangeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("SUBSCRIPTION_MANUAL_TIER_ENABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public sealed class SetTierRequest
    {
        public string Tier { get; init; } = string.Empty;
    }
}
