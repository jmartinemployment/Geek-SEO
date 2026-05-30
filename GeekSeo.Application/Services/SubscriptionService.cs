using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
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

    public async Task<Result<SeoSubscriptionSummary>> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await subscriptions.GetByUserIdAsync(userId, ct);
        if (!row.IsSuccess)
            return Result<SeoSubscriptionSummary>.Failure(row.Error ?? "Subscription lookup failed");

        if (row.Value is null)
        {
            return Result<SeoSubscriptionSummary>.Success(new SeoSubscriptionSummary(
                Tier: "starter",
                Status: "inactive",
                PaypalSubscriptionId: null,
                CurrentPeriodEnd: null));
        }

        return Result<SeoSubscriptionSummary>.Success(new SeoSubscriptionSummary(
            Tier: (row.Value.Tier ?? "none").ToLowerInvariant(),
            Status: (row.Value.Status ?? "inactive").ToLowerInvariant(),
            PaypalSubscriptionId: row.Value.PaypalSubscriptionId,
            CurrentPeriodEnd: row.Value.CurrentPeriodEnd));
    }

    public async Task<Result> ApplyPayPalSubscriptionAsync(
        Guid userId,
        string paypalSubscriptionId,
        string planId,
        string status,
        DateTimeOffset? currentPeriodEnd,
        CancellationToken ct = default)
    {
        var tier = MapPlanToTier(planId);
        if (tier is null)
            return Result.Failure($"Unknown PayPal plan id: {planId}");

        var upsert = await subscriptions.UpsertAsync(
            userId,
            new UpsertSubscriptionRequest
            {
                Tier = tier,
                Status = NormalizePayPalStatus(status),
                PaypalSubscriptionId = paypalSubscriptionId,
                CurrentPeriodEnd = currentPeriodEnd,
            },
            ct);
        return upsert.IsSuccess ? Result.Success() : Result.Failure(upsert.Error ?? "Subscription upsert failed");
    }

    public async Task<Result> DeactivateSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var upsert = await subscriptions.UpsertAsync(
            userId,
            new UpsertSubscriptionRequest
            {
                Tier = "starter",
                Status = "inactive",
                PaypalSubscriptionId = null,
                CurrentPeriodEnd = null,
            },
            ct);
        return upsert.IsSuccess ? Result.Success() : Result.Failure(upsert.Error ?? "Deactivate failed");
    }

    public async Task<Result> CancelPayPalSubscriptionAsync(Guid userId, CancellationToken ct = default)
    {
        var upsert = await subscriptions.UpsertAsync(
            userId,
            new UpsertSubscriptionRequest
            {
                Tier = "starter",
                Status = "cancelled",
                PaypalSubscriptionId = null,
                CurrentPeriodEnd = null,
            },
            ct);
        return upsert.IsSuccess ? Result.Success() : Result.Failure(upsert.Error ?? "Cancel failed");
    }

    internal static string? MapPlanToTier(string planId, PayPalPlanMap? map = null)
    {
        map ??= PayPalPlanMap.FromEnvironment();
        if (map is null)
            return null;

        if (string.Equals(planId, map.Starter, StringComparison.Ordinal))
            return "starter";
        if (string.Equals(planId, map.Professional, StringComparison.Ordinal))
            return "professional";
        if (string.Equals(planId, map.Team, StringComparison.Ordinal))
            return "team";
        if (string.Equals(planId, map.Agency, StringComparison.Ordinal))
            return "agency";
        return null;
    }

    private static string NormalizePayPalStatus(string status) =>
        string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ? "active" : "inactive";

    private static SubscriptionTier ParseTier(string tier) => tier.ToLowerInvariant() switch
    {
        "starter" => SubscriptionTier.Starter,
        "professional" => SubscriptionTier.Professional,
        "team" => SubscriptionTier.Team,
        "agency" => SubscriptionTier.Agency,
        _ => SubscriptionTier.None,
    };
}

public sealed record PayPalPlanMap(string Starter, string Professional, string Team, string Agency)
{
    public static PayPalPlanMap? FromEnvironment()
    {
        var starter = Environment.GetEnvironmentVariable("PAYPAL_PLAN_STARTER");
        var professional = Environment.GetEnvironmentVariable("PAYPAL_PLAN_PROFESSIONAL");
        var team = Environment.GetEnvironmentVariable("PAYPAL_PLAN_TEAM");
        var agency = Environment.GetEnvironmentVariable("PAYPAL_PLAN_AGENCY");
        if (string.IsNullOrWhiteSpace(starter)
            || string.IsNullOrWhiteSpace(professional)
            || string.IsNullOrWhiteSpace(team)
            || string.IsNullOrWhiteSpace(agency))
        {
            return null;
        }

        return new PayPalPlanMap(starter, professional, team, agency);
    }

    public IReadOnlyDictionary<string, string> ToPublicMap() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["starter"] = Starter,
            ["professional"] = Professional,
            ["team"] = Team,
            ["agency"] = Agency,
        };
}
