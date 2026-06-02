namespace GeekSeo.Application.Constants.Seo;

/// <summary>Monthly usage caps per tier. Keys match SeoUsageGateMiddleware feature keys.</summary>
public static class UsageLimits
{
    private static readonly Dictionary<string, int[]> Limits = new(StringComparer.Ordinal)
    {
        ["content_document"] = [20, 60, 150, int.MaxValue],
        ["content_brief"] = [20, 60, 150, int.MaxValue],
        ["ai_draft"] = [5, 20, 50, int.MaxValue],
        ["full_article"] = [3, 15, 40, int.MaxValue],
        ["humanize"] = [10, 50, int.MaxValue, int.MaxValue],
        ["ai_detect"] = [20, 100, int.MaxValue, int.MaxValue],
        ["keyword_lookup"] = [50, 200, 500, int.MaxValue],
        ["page_audit"] = [5, 20, int.MaxValue, int.MaxValue],
        ["site_audit"] = [0, 1, 3, int.MaxValue],
        ["deep_serp"] = [5, 30, 100, int.MaxValue],
        ["plagiarism_check"] = [10, 50, 150, int.MaxValue],
        ["auto_optimize"] = [20, 100, int.MaxValue, int.MaxValue],
        ["topical_map_refresh"] = [0, 2, 4, int.MaxValue],
        ["bulk_job"] = [0, 1, 3, int.MaxValue],
        // Outbound provider calls (Phase A.1 — SEO-PROVIDER-STRATEGY.md)
        [UsageFeatures.SerpFetch] = [100, 500, 2_000, int.MaxValue],
        [UsageFeatures.RankSnapshot] = [50, 400, 2_500, int.MaxValue],
        // Max enabled tracked keywords per project (enforced in RankTrackingService)
        [UsageFeatures.TrackedRankKeyword] = [5, 25, 100, 500],
    };

    public static int GetLimit(SubscriptionTier tier, string feature)
    {
        if (!Limits.TryGetValue(feature, out var caps))
            return int.MaxValue;
        var index = Math.Clamp((int)tier - 1, 0, 3);
        if (tier == SubscriptionTier.None)
            return 0;
        return caps[index];
    }
}
