using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class GeoScoringCalculatorTests
{
    [Fact]
    public void Calculate_returns_high_score_for_structured_cited_content()
    {
        var html = """
            <h1>Guide</h1>
            <h2>What is local SEO?</h2>
            <p>According to research shows, certified agencies improve rankings.</p>
            <ul><li>Step one</li><li>Step two</li></ul>
            <p><a href="https://example.org/study">Study</a> and <a href="https://gov.example">Gov</a>.</p>
            """;

        var plain = "Guide What is local SEO According to research shows certified agencies improve rankings Step one Step two Study Gov";
        var result = GeoScoringCalculator.Calculate(plain, html, wordCount: 1200, benchmarkWordCount: 1200);

        Assert.True(result.TotalScore >= 60);
        Assert.NotEqual("F", result.Grade);
        Assert.NotEmpty(result.Suggestions);
    }

    [Fact]
    public void Calculate_returns_low_score_for_thin_unstructured_content()
    {
        var result = GeoScoringCalculator.Calculate("short text", "<p>short text</p>", wordCount: 50, benchmarkWordCount: 1500);

        Assert.True(result.TotalScore < 40);
        Assert.Equal("F", result.Grade);
    }
}
