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

        Assert.Contains("Business Objectives", text);
        Assert.Contains("business outcomes or ROI", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pilot plan", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Movement 1 — Business Objectives", text);
        Assert.Contains("<p><strong>Movement", text);
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

    [Fact]
    public void HasRequiredBodySections_ReturnsFalseWhenH2sExistButMovementLabelsMissing()
    {
        var html =
            "<h2>Why automation matters</h2>" +
            "<h2>Data readiness</h2>" +
            "<h2>Tooling</h2>" +
            "<h2>Pilot plan</h2>";

        Assert.False(ArticleMethodologyOutlineEnricher.HasRequiredBodySections(html, WritingMethodologySpec.FourPhase));
    }
}

public sealed class ArticleMethodologyScaffoldTests
{
    [Fact]
    public void InjectMovementLabels_AddsVisibleMovementLinesBeforeH2s()
    {
        var html =
            "<h2>Why automation matters</h2><p>Body</p>" +
            "<h2>Data readiness</h2>" +
            "<h2>Tooling</h2>" +
            "<h2>Pilot plan</h2>";

        var result = ArticleMethodologyScaffold.InjectMovementLabels(
            html,
            "quickbooks automation",
            WritingMethodologySpec.FourPhase);

        Assert.Contains("Movement 1 — Business Objectives", result);
        Assert.Contains("Movement 4 — Pilot Implementation Strategy", result);
        Assert.True(ArticleMethodologyScaffold.HasVisibleMethodologyMovements(result, WritingMethodologySpec.FourPhase));
    }

    [Fact]
    public void BuildDeterministicBodySections_IncludesAllFourMovements()
    {
        var html = ArticleMethodologyScaffold.BuildDeterministicBodySections(
            "zapier quickbooks integration",
            WritingMethodologySpec.FourPhase);

        Assert.Contains("Movement 1 — Business Objectives", html);
        Assert.Contains("Movement 2 — Data Quality Assessment", html);
        Assert.Contains("Movement 3 — Tech Selection", html);
        Assert.Contains("Movement 4 — Pilot Implementation Strategy", html);
        Assert.Equal(4, ArticleMethodologyOutlineEnricher.CountBodyH2Sections(html));
    }
}
