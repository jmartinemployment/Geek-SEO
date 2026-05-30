using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class SubscriptionService(ISubscriptionRepository subscriptions) : ISubscriptionService
{
    public async Task<Result<SubscriptionTier>> GetActiveTierAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await subscriptions.GetByUserIdAsync(userId, ct);
        if (!row.IsSuccess)
            return Result<SubscriptionTier>.Failure(row.Error ?? "Subscription lookup failed");

        if (row.Value is null)
            return Result<SubscriptionTier>.Success(SubscriptionTier.Starter);

        if (!string.Equals(row.Value.Status, "active", StringComparison.OrdinalIgnoreCase))
            return Result<SubscriptionTier>.Success(SubscriptionTier.None);

        return Result<SubscriptionTier>.Success(ParseTier(row.Value.Tier ?? "none"));
    }

    private static SubscriptionTier ParseTier(string tier) => tier.ToLowerInvariant() switch
    {
        "starter" => SubscriptionTier.Starter,
        "professional" => SubscriptionTier.Professional,
        "team" => SubscriptionTier.Team,
        "agency" => SubscriptionTier.Agency,
        _ => SubscriptionTier.None,
    };
}
