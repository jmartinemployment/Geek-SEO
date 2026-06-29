using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class FaqAnswerValidatorTests
{
    [Fact]
    public void ValidateSingle_inactive_assignment_strips_anchors()
    {
        var assignment = new LinkedFaqAssignment(
            "faq-01",
            "Are there free AI tools for market research?",
            string.Empty,
            "free AI tools for market research",
            IsTargetActive: false);

        var result = FaqAnswerValidator.ValidateSingle(
            assignment,
            "Yes — explore our <a href=\"/blog/free-tools\">free AI tools for market research</a> first.");

        Assert.DoesNotContain("<a ", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("free AI tools for market research", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateSingle_active_assignment_injects_registry_href_when_missing()
    {
        var assignment = new LinkedFaqAssignment(
            "faq-01",
            "Which AI tools are best for market research?",
            "/blog/best-ai-tools-market-research",
            "best AI tools for market research",
            IsTargetActive: true);

        var result = FaqAnswerValidator.ValidateSingle(
            assignment,
            "Teams should compare vendors carefully before committing budget.");

        Assert.Contains("href=\"/blog/best-ai-tools-market-research\"", result, StringComparison.Ordinal);
        Assert.Contains(">best AI tools for market research</a>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateSingle_strips_hallucinated_href_and_injects_registry_link()
    {
        var assignment = new LinkedFaqAssignment(
            "faq-01",
            "Which AI tools are best for market research?",
            "/blog/best-ai-tools-market-research",
            "best AI tools for market research",
            IsTargetActive: true);

        var result = FaqAnswerValidator.ValidateSingle(
            assignment,
            "See our <a href=\"/blog/wrong-slug\">best AI tools for market research</a> guide.");

        Assert.Contains("href=\"/blog/best-ai-tools-market-research\"", result, StringComparison.Ordinal);
        Assert.DoesNotContain("/blog/wrong-slug", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateSingle_rejects_javascript_scheme()
    {
        var assignment = new LinkedFaqAssignment(
            "faq-01",
            "Which AI tools are best?",
            "/blog/best-ai-tools",
            "best AI tools",
            IsTargetActive: true);

        var result = FaqAnswerValidator.ValidateSingle(
            assignment,
            "Bad <a href=\"javascript:alert(1)\">link</a>.");

        Assert.Equal(string.Empty, result);
    }
}
