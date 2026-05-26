using GeekSeoBackend.Auth;
using GeekApplication.Constants.Seo;
using GeekApplication.Interfaces.Seo;

namespace GeekSeoBackend.Middleware;

public sealed class SeoFeatureGateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptions, ICurrentUserContext userContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/seo", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var required = FeatureGates.GetRequiredTier(path);
        if (required is null)
        {
            await next(context);
            return;
        }

        Guid userId;
        try
        {
            userId = userContext.UserId;
        }
        catch
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required" });
            return;
        }

        var tierResult = await subscriptions.GetActiveTierAsync(userId);
        if (!tierResult.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = tierResult.Error });
            return;
        }

        var currentTier = tierResult.Value;
        var requiredTier = required!.Value;
        if ((int)currentTier < (int)requiredTier)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Upgrade required for this feature",
                requiredTier = requiredTier.ToString().ToLowerInvariant(),
                currentTier = currentTier.ToString().ToLowerInvariant(),
                upgradeUrl = "https://seo.geekatyourspot.com/pricing",
            });
            return;
        }

        await next(context);
    }
}
