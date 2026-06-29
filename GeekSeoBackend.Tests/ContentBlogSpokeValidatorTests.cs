using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentBlogSpokeValidatorTests
{
    [Fact]
    public void Validate_rejects_substring_keyword_collision()
    {
        var spoke = new ContentBlogSpoke
        {
            Title = "What AI content tooling costs",
            Slug = "what-ai-content-tooling-costs",
            PrimaryKeyword = "AI content operations implementation",
            SpokeType = "cost",
            ContentHtml = "<p>" + new string('x', 220) + "</p>",
        };

        var result = ContentBlogSpokeValidator.Validate("AI content operations", spoke);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("collision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_accepts_distinct_spoke_keyword()
    {
        var spoke = new ContentBlogSpoke
        {
            Title = "What AI content tooling costs",
            Slug = "what-ai-content-tooling-costs",
            PrimaryKeyword = "what AI content tooling actually costs",
            SpokeType = "cost",
            ContentHtml = "<p>" + new string('x', 220) + "</p>",
        };

        var result = ContentBlogSpokeValidator.Validate("AI content operations", spoke);

        Assert.True(result.IsValid);
    }
}
