using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentMarketingValidatorTests
{
    [Fact]
    public void Validate_fails_when_summaries_are_identical()
    {
        var bundle = new ContentMarketingBundle
        {
            DepartmentSlug = "marketing",
            UseCaseSlug = "content-operations",
            PrimaryKeyword = "AI content operations",
            HomeSummary = "Same text here for all three fields in test.",
            HubSummary = "Same text here for all three fields in test.",
            MetaDescription = "Different meta description that is long enough to pass.",
        };

        var result = ContentMarketingValidator.Validate(bundle);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("homeSummary and hubSummary", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_fails_on_pillar_spoke_keyword_substring_collision()
    {
        var bundle = ValidBundle();
        bundle.BlogSpoke = new ContentMarketingBlogSpoke
        {
            Slug = "ai-content-operations-implementation",
            PrimaryKeyword = "AI content operations implementation",
            SpokeType = "how-to",
            Title = "Implementation guide",
            ContentHtml = "<p>Body</p>",
        };

        var result = ContentMarketingValidator.Validate(bundle);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("substring collision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_passes_for_distinct_summaries_and_spoke_keyword()
    {
        var bundle = ValidBundle();
        bundle.BlogSpoke = new ContentMarketingBlogSpoke
        {
            Slug = "what-ai-content-tooling-costs",
            PrimaryKeyword = "what AI content tooling actually costs",
            SpokeType = "cost",
            Title = "What AI content tooling actually costs",
            ContentHtml = "<p>Body</p>",
            Excerpt = "Compare approaches.",
            MetaDescription = "See how AI content operations compares to manual workflows for South Florida SMB teams.",
        };

        var result = ContentMarketingValidator.Validate(bundle);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static ContentMarketingBundle ValidBundle() => new()
    {
        DepartmentSlug = "marketing",
        UseCaseSlug = "content-operations",
        PrimaryKeyword = "AI content operations",
        HomeSummary = "AI content ops cuts audit time from hours to minutes so small teams publish at enterprise scale.",
        HubSummary = "AI content operations integrates automation across planning, production, distribution, and analytics.",
        MetaDescription = "Learn how AI content operations helps South Florida SMBs scale quality content without adding headcount or sacrificing governance.",
    };
}
