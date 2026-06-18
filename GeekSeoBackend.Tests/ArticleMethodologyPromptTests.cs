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
            WritingMethodologySpec.FivePhase);

        Assert.Contains("Section 1 intent:", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("business outcomes or ROI", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pilot plan", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scaling safely", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Section 5 intent:", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Heading families", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Movement 1 —", text);
        Assert.DoesNotContain("<p><strong>Movement", text);
    }

    [Fact]
    public void BuildOutlineRepairUserPrompt_RequestsFiveBodySectionsOnly()
    {
        var text = ArticleMethodologyPrompt.BuildOutlineRepairUserPrompt(
            "zapier quickbooks integration",
            WritingMethodologySpec.FivePhase,
            "<h2>Weak section</h2>",
            ["How to connect QuickBooks"]);

        Assert.Contains("Output only the 5 body sections", text);
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
            "<h2>Scaling safely across teams</h2>" +
            $"<h2>{ContentWritingRules.ClosingFaqHeading}</h2>" +
            "<h3>What is automation?</h3>";

        Assert.Equal(5, ArticleMethodologyOutlineEnricher.CountBodyH2Sections(html));
    }

    [Fact]
    public void HasRequiredBodySections_ReturnsFalseWhenFewerThanFiveBodyH2s()
    {
        var html = "<h2>Only one section</h2>";
        Assert.False(ArticleMethodologyOutlineEnricher.HasRequiredBodySections(html, WritingMethodologySpec.FivePhase));
    }

    [Fact]
    public void HasRequiredBodySections_ReturnsTrueWhenFiveBodyH2sPresent()
    {
        var html =
            "<h2>Why automation matters</h2>" +
            "<h2>Data readiness</h2>" +
            "<h2>Tooling</h2>" +
            "<h2>Pilot plan</h2>" +
            "<h2>Scaling safely</h2>";

        Assert.True(ArticleMethodologyOutlineEnricher.HasRequiredBodySections(html, WritingMethodologySpec.FivePhase));
    }
}

public sealed class ArticleMethodologyScaffoldTests
{
    [Fact]
    public void StripMovementLabels_RemovesInternalLabels()
    {
        var html =
            "<p><strong>Movement 1 — Business Objectives</strong></p>" +
            "<h2>Why automation matters</h2><p>Body</p>" +
            "<p><strong>Movement 2 — Data Quality Assessment</strong></p>" +
            "<h2>Data readiness</h2>";

        var result = ArticleMethodologyScaffold.StripMovementLabels(html);

        Assert.DoesNotContain("Movement 1", result);
        Assert.DoesNotContain("Movement 2", result);
        Assert.Contains("<h2>Why automation matters</h2>", result);
    }

    [Fact]
    public void StripMovementLabels_RemovesMovementHeadingsAndKeepsReaderFacingH2s()
    {
        var html =
            "<h2>Movement 1 — Business Objectives</h2>" +
            "<h2>Defining Goals and Success Metrics for Bookkeeping Automation</h2><p>Intro</p>" +
            "<h2>Movement 2 — Data Quality Assessment</h2>" +
            "<h2>Movement 3 — Tech Selection</h2>" +
            "<h2>Choosing the Right Automated Data Entry Software</h2><p>Tools</p>" +
            "<h2>Movement 4 — Pilot Implementation Strategy</h2>" +
            "<h2>Rolling Out Automated Bookkeeping in Phases</h2><p>Pilot</p>";

        var result = ArticleMethodologyScaffold.StripMovementLabels(html);

        Assert.DoesNotContain("Movement", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Defining Goals and Success Metrics", result);
        Assert.Contains("Choosing the Right Automated Data Entry Software", result);
        Assert.Contains("Rolling Out Automated Bookkeeping in Phases", result);
    }

    [Fact]
    public void SanitizeDraft_StripsMovementLabelsBeforeStructureRepair()
    {
        var html =
            "<h1>Bookkeeping automation</h1>" +
            "<h2>Movement 1 — Business Objectives</h2>" +
            "<h2>Only one real section</h2><p>Body</p>";

        var result = ArticleMethodologyScaffold.SanitizeDraft(
            html,
            "bookkeeping automation",
            WritingMethodologySpec.FivePhase);

        Assert.DoesNotContain("Movement", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(ArticleMethodologyOutlineEnricher.HasRequiredBodySections(result, WritingMethodologySpec.FivePhase));
    }

    [Fact]
    public void BuildDeterministicBodySections_IncludesFiveTopicHeadings()
    {
        var html = ArticleMethodologyScaffold.BuildDeterministicBodySections(
            "zapier quickbooks integration",
            WritingMethodologySpec.FivePhase);

        Assert.DoesNotContain("Movement 1", html);
        Assert.Contains("<h2>", html);
        Assert.Equal(5, ArticleMethodologyOutlineEnricher.CountBodyH2Sections(html));
    }
}
