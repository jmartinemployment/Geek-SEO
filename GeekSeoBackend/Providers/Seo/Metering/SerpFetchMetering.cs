using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;

namespace GeekSeoBackend.Providers.Seo.Metering;

/// <summary>
/// Increments <see cref="UsageFeatures.SerpFetch"/> for background SERP calls (topical map, GEO).
/// User-facing deep SERP uses route middleware (<see cref="UsageFeatures.DeepSerp"/>) to avoid double counting.
/// </summary>
internal static class SerpFetchMetering
{
    internal static async Task TryIncrementAsync(
        IUsageMeteringService metering,
        ICurrentUserContext userContext,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            var userId = userContext.UserId;
            if (userId == Guid.Empty)
                return;

            var increment = await metering.IncrementAsync(userId, UsageFeatures.SerpFetch, 1, ct);
            if (!increment.IsSuccess)
            {
                logger.LogWarning(
                    "serp_fetch metering increment failed for user {UserId}: {Error}",
                    userId,
                    increment.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "serp_fetch metering skipped");
        }
    }
}
