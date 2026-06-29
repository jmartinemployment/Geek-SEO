using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentPublishSlugTests
{
    [Theory]
    [InlineData("Best AI Tools for Market Research", "best-ai-tools-for-market-research")]
    [InlineData("  AI   Market   Intelligence  ", "ai-market-intelligence")]
    [InlineData("companies & analytics!", "companies-analytics")]
    public void NormalizeFromPhrase_produces_kebab_case(string phrase, string expected)
    {
        Assert.Equal(expected, ContentPublishSlug.NormalizeFromPhrase(phrase));
    }

    [Theory]
    [InlineData("best-ai-tools-market-research", true)]
    [InlineData("ai-market-research-2", true)]
    [InlineData("AI-Market-Research", false)]
    [InlineData("bad slug", false)]
    public void IsValid_enforces_lowercase_kebab_pattern(string slug, bool expected)
    {
        Assert.Equal(expected, ContentPublishSlug.IsValid(slug));
    }

    [Fact]
    public void AllocateUnique_appends_numeric_suffix_on_collision()
    {
        var existing = new[] { "best-ai-tools", "best-ai-tools-2" };

        Assert.Equal(
            "best-ai-tools-3",
            ContentPublishSlug.AllocateUnique("best-ai-tools", existing));
    }

    [Fact]
    public void AllocateUnique_returns_base_when_available()
    {
        Assert.Equal(
            "best-ai-tools",
            ContentPublishSlug.AllocateUnique("best-ai-tools", ["other-slug"]));
    }
}
