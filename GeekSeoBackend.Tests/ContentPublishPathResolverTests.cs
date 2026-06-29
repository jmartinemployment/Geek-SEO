using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentPublishPathResolverTests
{
    [Theory]
    [InlineData("best-ai-tools-market-research", "/blog/best-ai-tools-market-research")]
    [InlineData("AI-Market-Research", "/blog/ai-market-research")]
    public void ResolveRelativePath_returns_blog_prefixed_path(string slug, string expected)
    {
        Assert.Equal(expected, ContentPublishPathResolver.ResolveRelativePath(slug));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a slug")]
    [InlineData("-leading")]
    public void ResolveRelativePath_returns_null_for_invalid_slug(string? slug)
    {
        Assert.Null(ContentPublishPathResolver.ResolveRelativePath(slug));
    }

    [Fact]
    public void ResolveAbsoluteUrl_joins_project_url_and_relative_path()
    {
        Assert.Equal(
            "https://geekatyourspot.com/blog/best-ai-tools-market-research",
            ContentPublishPathResolver.ResolveAbsoluteUrl(
                "https://geekatyourspot.com/",
                "best-ai-tools-market-research"));
    }

    [Fact]
    public void ResolveAbsoluteUrl_returns_relative_when_project_url_missing()
    {
        Assert.Equal(
            "/blog/best-ai-tools-market-research",
            ContentPublishPathResolver.ResolveAbsoluteUrl(null, "best-ai-tools-market-research"));
    }
}
