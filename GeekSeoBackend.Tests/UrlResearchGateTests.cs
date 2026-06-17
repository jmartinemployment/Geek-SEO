using GeekSeo.Application.Constants.Seo;

namespace GeekSeoBackend.Tests;

public sealed class UrlResearchGateTests
{
    [Fact]
    public void MeteredRoutes_maps_post_analyze_to_url_research_analyze()
    {
        var feature = MeteredRoutes.GetFeatureKey(
            "POST",
            "/api/seo/url-research/analyze");

        Assert.Equal(UsageFeatures.UrlResearchAnalyze, feature);
    }

    [Fact]
    public void MeteredRoutes_does_not_meter_get_by_id()
    {
        var feature = MeteredRoutes.GetFeatureKey(
            "GET",
            "/api/seo/url-research/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        Assert.Null(feature);
    }

    [Theory]
    [InlineData(SubscriptionTier.Starter, 5)]
    [InlineData(SubscriptionTier.Professional, 20)]
    [InlineData(SubscriptionTier.Team, 60)]
    public void UsageLimits_define_monthly_url_research_analyze_caps(SubscriptionTier tier, int expected)
    {
        Assert.Equal(expected, UsageLimits.GetLimit(tier, UsageFeatures.UrlResearchAnalyze));
    }
}
