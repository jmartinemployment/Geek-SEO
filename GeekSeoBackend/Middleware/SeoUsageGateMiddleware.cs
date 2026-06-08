using GeekSeoBackend.Auth;
using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using Microsoft.Extensions.Caching.Memory;

namespace GeekSeoBackend.Middleware;

public sealed class SeoUsageGateMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan TierCacheTtl = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(
        HttpContext context,
        IUsageMeteringService metering,
        ISubscriptionService subscriptions,
        ICurrentUserContext userContext,
        IMemoryCache cache)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var feature = MeteredRoutes.GetFeatureKey(context.Request.Method, path);
        if (feature is null)
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

        Result<SubscriptionTier> tierResult;
        var tierCacheKey = $"tier:{userId}";
        if (cache.TryGetValue(tierCacheKey, out SubscriptionTier cachedTier))
        {
            tierResult = Result<SubscriptionTier>.Success(cachedTier);
        }
        else
        {
            try
            {
                tierResult = await subscriptions.GetActiveTierAsync(userId);
                if (tierResult.IsSuccess)
                    cache.Set(tierCacheKey, tierResult.Value!, TierCacheTtl);
            }
            catch
            {
                tierResult = Result<SubscriptionTier>.Success(SubscriptionTier.Starter);
            }
        }

        if (!tierResult.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = tierResult.Error });
            return;
        }

        Result ensure;
        try
        {
            ensure = await metering.EnsureWithinLimitAsync(userId, tierResult.Value!, feature);
        }
        catch
        {
            ensure = Result.Success();
        }
        if (!ensure.IsSuccess)
        {
            var usage = await metering.GetUsageAsync(userId, feature);
            var limit = await metering.GetLimitAsync(tierResult.Value!, feature);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = ensure.Error,
                feature,
                usage = usage.Value,
                limit = limit.Value,
                upgradeUrl = "https://seo.geekatyourspot.com/pricing",
            });
            return;
        }

        await next(context);

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            await metering.IncrementAsync(userId, feature);
        }
    }
}
