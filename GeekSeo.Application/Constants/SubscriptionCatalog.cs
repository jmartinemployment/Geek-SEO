namespace GeekSeo.Application.Constants.Seo;

public sealed record SubscriptionCatalogTier(
    string Key,
    string Name,
    string PriceLabel,
    int PriceMonthly,
    IReadOnlyList<string> Highlights,
    SubscriptionTier Tier);

public static class SubscriptionCatalog
{
    public static readonly IReadOnlyList<SubscriptionCatalogTier> Tiers =
    [
        new(
            Key: "starter",
            Name: "Starter",
            PriceLabel: "$29",
            PriceMonthly: 29,
            Highlights:
            [
                "20 documents",
                "3 full articles",
                "Local SERP on every tier",
            ],
            Tier: SubscriptionTier.Starter),
        new(
            Key: "professional",
            Name: "Professional",
            PriceLabel: "$59",
            PriceMonthly: 59,
            Highlights:
            [
                "GSC + GA4",
                "Topical map",
                "Content audit",
            ],
            Tier: SubscriptionTier.Professional),
        new(
            Key: "team",
            Name: "Team",
            PriceLabel: "$89",
            PriceMonthly: 89,
            Highlights:
            [
                "Bulk jobs",
                "Content Guard",
                "Higher GEO limits",
            ],
            Tier: SubscriptionTier.Team),
        new(
            Key: "agency",
            Name: "Agency",
            PriceLabel: "$149",
            PriceMonthly: 149,
            Highlights:
            [
                "Public API",
                "Unlimited caps",
                "White-label reports",
            ],
            Tier: SubscriptionTier.Agency),
    ];

    public static string NormalizeTierKey(string? tierKey) =>
        (tierKey ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValidTierKey(string? tierKey)
    {
        var normalized = NormalizeTierKey(tierKey);
        foreach (var tier in Tiers)
        {
            if (string.Equals(tier.Key, normalized, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
