using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeoBackend.Auth;

namespace GeekSeoBackend.Services;

/// <summary>
/// Grants configured operator emails/user IDs full Agency access; everyone else uses paid tier from DB/PayPal.
/// </summary>
public sealed class FullAccessSubscriptionService(
    SubscriptionService inner,
    ICurrentUserContext user) : ISubscriptionService
{
    private bool HasFullAccess(Guid userId) =>
        SubscriptionFullAccess.IsGranted(userId, user.Email);

    public Task<Result<SubscriptionTier>> GetActiveTierAsync(Guid userId, CancellationToken ct = default)
    {
        if (HasFullAccess(userId))
            return Task.FromResult(Result<SubscriptionTier>.Success(SubscriptionTier.Agency));

        return inner.GetActiveTierAsync(userId, ct);
    }

    public Task<Result<SeoSubscriptionSummary>> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        if (HasFullAccess(userId))
        {
            return Task.FromResult(Result<SeoSubscriptionSummary>.Success(new SeoSubscriptionSummary(
                Tier: "agency",
                Status: "active",
                PaypalSubscriptionId: null,
                CurrentPeriodEnd: null)));
        }

        return inner.GetSummaryAsync(userId, ct);
    }

    public Task<Result> ApplyPayPalSubscriptionAsync(
        Guid userId,
        string paypalSubscriptionId,
        string planId,
        string status,
        DateTimeOffset? currentPeriodEnd,
        CancellationToken ct = default) =>
        inner.ApplyPayPalSubscriptionAsync(userId, paypalSubscriptionId, planId, status, currentPeriodEnd, ct);

    public Task<Result> DeactivateSubscriptionAsync(Guid userId, CancellationToken ct = default) =>
        inner.DeactivateSubscriptionAsync(userId, ct);

    public Task<Result> CancelPayPalSubscriptionAsync(Guid userId, CancellationToken ct = default) =>
        inner.CancelPayPalSubscriptionAsync(userId, ct);

    public Task<Result> SetTierManuallyAsync(Guid userId, string tierKey, CancellationToken ct = default) =>
        inner.SetTierManuallyAsync(userId, tierKey, ct);
}
