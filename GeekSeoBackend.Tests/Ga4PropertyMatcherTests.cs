using GeekSeoBackend.Services;

namespace GeekSeoBackend.Tests;

public sealed class Ga4PropertyMatcherTests
{
    [Fact]
    public void Match_SelectsProperty_WithMatchingWebStream()
    {
        var candidates = new[]
        {
            new Ga4PropertyCandidate("111", "Other Site", ["https://www.example.com"]),
            new Ga4PropertyCandidate("519414748", "Geek At Your Spot", ["https://www.geekatyourspot.com"]),
        };

        var matched = Ga4PropertyMatcher.Match(candidates, "https://geekatyourspot.com/", "111");

        Assert.Equal("519414748", matched);
    }

    [Fact]
    public void NormalizePropertyId_StripsPropertiesPrefix()
    {
        Assert.Equal("519414748", Ga4PropertyMatcher.NormalizePropertyId("properties/519414748"));
    }

    [Fact]
    public void Match_UsesPreferredProperty_WhenItMatchesSite()
    {
        var candidates = new[]
        {
            new Ga4PropertyCandidate("519414748", "Geek", ["https://www.geekatyourspot.com"]),
            new Ga4PropertyCandidate("999", "Geek Backup", ["https://www.geekatyourspot.com"]),
        };

        var matched = Ga4PropertyMatcher.Match(candidates, "https://www.geekatyourspot.com/", "519414748");

        Assert.Equal("519414748", matched);
    }
}
