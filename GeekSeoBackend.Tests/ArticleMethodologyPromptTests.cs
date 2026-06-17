using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ArticleMethodologyPromptTests
{
    [Fact]
    public void BuildWeaveInstructions_IncludesIntentAndHeadingFamilies()
    {
        var text = ArticleMethodologyPrompt.BuildWeaveInstructions(
            "quickbooks automation consultant",
            WritingMethodologySpec.FourPhase);

        Assert.Contains("business-objectives", text);
        Assert.Contains("Business Objectives", text);
        Assert.Contains("business outcomes or ROI", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pilot plan", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do NOT copy the corporate phase labels", text);
        Assert.Contains("<!-- methodology:business-objectives -->", text);
    }

    [Fact]
    public void BuildOutlineRepairUserPrompt_RequestsFourBodySectionsOnly()
    {
        var text = ArticleMethodologyPrompt.BuildOutlineRepairUserPrompt(
            "zapier quickbooks integration",
            WritingMethodologySpec.FourPhase,
            "<h2>Weak section</h2>",
            ["How to connect QuickBooks"]);

        Assert.Contains("Output only the four methodology body sections", text);
        Assert.Contains("zapier quickbooks integration", text);
        Assert.Contains("How to connect QuickBooks", text);
    }
}

public sealed class ArticleMethodologyOutlineEnricherTests
{
    [Fact]
    public void CountBodyH2Sections_ExcludesFaqSection()
    {
        var html =
            "<h2>Why automation matters for finance teams</h2>" +
            "<h2>Data readiness checklist</h2>" +
            "<h2>Tooling options</h2>" +
            "<h2>Pilot rollout plan</h2>" +
            $"<h2>{ContentWritingRules.ClosingFaqHeading}</h2>" +
            "<h3>What is automation?</h3>";

        Assert.Equal(4, ArticleMethodologyOutlineEnricher.CountBodyH2Sections(html));
    }

    [Fact]
    public void HasRequiredBodySections_ReturnsFalseWhenFewerThanFourBodyH2s()
    {
        var html = "<h2>Only one section</h2>";
        Assert.False(ArticleMethodologyOutlineEnricher.HasRequiredBodySections(html, WritingMethodologySpec.FourPhase));
    }
}
