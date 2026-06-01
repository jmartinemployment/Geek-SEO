using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Auth;

namespace GeekSeoBackend.Providers.Seo.Metering;

/// <summary>Counts successful rank snapshot provider calls against monthly <see cref="UsageFeatures.RankSnapshot"/>.</summary>
public sealed class MeteredRankSnapshotProvider(
    IRankSnapshotProvider inner,
    IUsageMeteringService metering,
    ICurrentUserContext userContext,
    ILogger<MeteredRankSnapshotProvider> logger) : IRankSnapshotProvider
{
    public string ProviderName => inner.ProviderName;

    public async Task<Result<RankSnapshot>> GetRankAsync(
        string keyword,
        string domain,
        string location,
        CancellationToken ct = default)
    {
        var result = await inner.GetRankAsync(keyword, domain, location, ct);
        if (!result.IsSuccess)
            return result;

        await TryIncrementAsync(ct);
        return result;
    }

    private async Task TryIncrementAsync(CancellationToken ct)
    {
        try
        {
            var userId = userContext.UserId;
            if (userId == Guid.Empty)
                return;

            var increment = await metering.IncrementAsync(userId, UsageFeatures.RankSnapshot, 1, ct);
            if (!increment.IsSuccess)
            {
                logger.LogWarning(
                    "Rank snapshot metering increment failed for user {UserId}: {Error}",
                    userId,
                    increment.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rank snapshot metering skipped");
        }
    }
}
