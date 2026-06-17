using GeekSeo.Application.Infrastructure;

namespace GeekSeoBackend.Tests;

public sealed class AiDetectionResponseParserTests
{
    [Fact]
    public void TryParse_reads_plain_json()
    {
        var ok = AiDetectionResponseParser.TryParse(
            """{"aiProbability":0.82,"summary":"Reads like templated AI copy."}""",
            out var probability,
            out var summary);

        Assert.True(ok);
        Assert.Equal(0.82, probability);
        Assert.Equal("Reads like templated AI copy.", summary);
    }

    [Fact]
    public void TryParse_reads_markdown_wrapped_json()
    {
        var ok = AiDetectionResponseParser.TryParse(
            """
            ```json
            {"aiProbability":0.35,"summary":"Mixed human and AI phrasing."}
            ```
            """,
            out var probability,
            out var summary);

        Assert.True(ok);
        Assert.Equal(0.35, probability);
        Assert.Equal("Mixed human and AI phrasing.", summary);
    }

    [Fact]
    public void TryParse_accepts_snake_case_probability_field()
    {
        var ok = AiDetectionResponseParser.TryParse(
            """{"ai_probability":"0.6","summary":"Likely AI-assisted."}""",
            out var probability,
            out _);

        Assert.True(ok);
        Assert.Equal(0.6, probability);
    }
}
