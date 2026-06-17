using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class RegistrableDomainMatcherTests
{
    [Theory]
    [InlineData("https://blog.example.com/page", "https://example.com", true)]
    [InlineData("https://www.example.com/a", "https://example.com/b", true)]
    [InlineData("https://example.com", "https://shop.example.com", true)]
    [InlineData("https://other.com", "https://example.com", false)]
    [InlineData("not-a-url", "https://example.com", false)]
    public void SameRegistrableDomain_MatchesExpected(string pageUrl, string projectUrl, bool expected) =>
        Assert.Equal(expected, RegistrableDomainMatcher.SameRegistrableDomain(pageUrl, projectUrl));
}
