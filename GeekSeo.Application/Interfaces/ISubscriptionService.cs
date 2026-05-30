using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISubscriptionService
{
    Task<Result<SubscriptionTier>> GetActiveTierAsync(Guid userId, CancellationToken ct = default);
    Task<Result<SeoSubscriptionSummary>> GetSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<Result> ApplyPayPalSubscriptionAsync(
        Guid userId,
        string paypalSubscriptionId,
        string planId,
        string status,
        DateTimeOffset? currentPeriodEnd,
        CancellationToken ct = default);
    Task<Result> DeactivateSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<Result> CancelPayPalSubscriptionAsync(Guid userId, CancellationToken ct = default);
}

public sealed record SeoSubscriptionSummary(
    string Tier,
    string Status,
    string? PaypalSubscriptionId,
    DateTimeOffset? CurrentPeriodEnd);
