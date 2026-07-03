using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SerpSearchKeywordNormalizerTests
{
    [Theory]
    [InlineData("AI for Prospecting site:wiki", "AI for Prospecting")]
    [InlineData("ai bookkeeping site:en.wikipedia.org", "ai bookkeeping")]
    [InlineData("widget repair filetype:pdf", "widget repair")]
    [InlineData("plain keyword", "plain keyword")]
    public void Normalize_strips_google_search_operators(string input, string expected)
    {
        Assert.Equal(expected, SerpSearchKeywordNormalizer.Normalize(input));
    }

    [Fact]
    public void ContainsSearchOperators_detects_site_colon()
    {
        Assert.True(SerpSearchKeywordNormalizer.ContainsSearchOperators("AI site:wiki"));
        Assert.False(SerpSearchKeywordNormalizer.ContainsSearchOperators("AI prospecting"));
    }
}
