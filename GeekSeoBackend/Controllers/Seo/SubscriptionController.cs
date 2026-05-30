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
        var config = paypal.GetCheckoutConfig();
        if (config is null)
            return Ok(new { configured = false });

        return Ok(new
        {
            configured = true,
            clientId = config.ClientId,
            planIds = config.PlanIds,
        });
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
}
