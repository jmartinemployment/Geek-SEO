using GeekSeoBackend.Extensions;

namespace GeekSeoBackend.Tests;

public sealed class CorsOriginParserOwnOriginTests
{
    [Theory]
    [InlineData("https://geekatyourspot.com")]
    [InlineData("https://geek-content-creator.geekatyourspot.com")]
    [InlineData("https://geek-image-generator.geekatyourspot.com")]
    [InlineData("https://SEO.GEEKATYOURSPOT.COM")]
    public void Own_https_hosts_are_allowed(string origin) =>
        Assert.True(CorsOriginParser.IsOwnOrigin(origin));

    [Theory]
    // A lookalike registered by someone else must not match on suffix alone.
    [InlineData("https://geekatyourspot.com.evil.test")]
    [InlineData("https://notgeekatyourspot.com")]
    [InlineData("https://evil.test")]
    // http is not enough: credentials ride on these requests.
    [InlineData("http://geekatyourspot.com")]
    [InlineData("http://geek-content-creator.geekatyourspot.com")]
    [InlineData("not-a-url")]
    [InlineData("")]
    public void Everything_else_is_rejected(string origin) =>
        Assert.False(CorsOriginParser.IsOwnOrigin(origin));

    [Fact]
    public void Defaults_include_the_content_creator_custom_domain()
    {
        Environment.SetEnvironmentVariable("CORS_ORIGINS", null);
        Assert.Contains(
            "https://geek-content-creator.geekatyourspot.com",
            CorsOriginParser.GetAllowedOrigins());
    }
}
