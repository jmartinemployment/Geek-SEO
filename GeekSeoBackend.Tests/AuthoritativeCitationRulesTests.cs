using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class AuthoritativeCitationRulesTests
{
    [Theory]
    [InlineData("https://www.cdc.gov/flu", true)]
    [InlineData("https://www.ed.gov/policy", true)]
    [InlineData("https://www.nist.gov/publications", true)]
    [InlineData("https://competitor.com/ai-customer-journey", false)]
    [InlineData("https://www.reddit.com/r/seo", false)]
    public void IsAcceptableDiscoveredCitationUrl_filters_hosts(string url, bool expected)
    {
        Assert.Equal(expected, AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(url));
    }
}
