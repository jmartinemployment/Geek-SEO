using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class ContentBodyLinkInserterTests
{
  private const string SampleHtml = """
        <h2 id="implementation">Implementation approach</h2>
        <p>Teams often compare free AI tools for market research before committing.</p>
        <h2>Outcomes</h2>
        <p>Measure ROI carefully.</p>
        """;

    [Fact]
    public void ApplyBodyLinks_section_footer_inserts_related_guide_box()
    {
        var (result, applied) = ContentBodyLinkInserter.ApplyBodyLinks(SampleHtml,
        [
            new BodyLinkInsertionInstruction
            {
                LinkId = "body-01",
                TargetHeadingId = "implementation",
                PlacementStrategy = BodyLinkPlacementStrategy.SectionFooter,
                TargetPath = "/blog/best-ai-tools-market-research",
                AnchorText = "best AI tools for market research",
                IsTargetActive = true,
            },
        ]);

        Assert.Equal(1, applied);

        Assert.Contains("related-guide-box", result, StringComparison.Ordinal);
        Assert.Contains("href=\"/blog/best-ai-tools-market-research\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyBodyLinks_skips_inactive_targets()
    {
        var (result, applied) = ContentBodyLinkInserter.ApplyBodyLinks(SampleHtml,
        [
            new BodyLinkInsertionInstruction
            {
                LinkId = "body-01",
                TargetHeadingId = "implementation",
                PlacementStrategy = BodyLinkPlacementStrategy.SectionFooter,
                TargetPath = "/blog/best-ai-tools-market-research",
                AnchorText = "best AI tools for market research",
                IsTargetActive = false,
            },
        ]);

        Assert.Equal(0, applied);

        Assert.Equal(SampleHtml, result);
    }

    [Fact]
    public void ApplyBodyLinks_replace_existing_text_wraps_anchor_phrase()
    {
        var (result, applied) = ContentBodyLinkInserter.ApplyBodyLinks(SampleHtml,
        [
            new BodyLinkInsertionInstruction
            {
                LinkId = "body-02",
                TargetHeadingId = "implementation",
                PlacementStrategy = BodyLinkPlacementStrategy.ReplaceExistingText,
                TargetPath = "/blog/free-ai-market-research-tools",
                AnchorText = "free AI tools for market research",
                IsTargetActive = true,
            },
        ]);

        Assert.Equal(1, applied);

        Assert.Contains(
            "<a href=\"/blog/free-ai-market-research-tools\">free AI tools for market research</a>",
            result,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyBodyLinks_append_to_paragraph_adds_context_phrase()
    {
        var (result, applied) = ContentBodyLinkInserter.ApplyBodyLinks(SampleHtml,
        [
            new BodyLinkInsertionInstruction
            {
                LinkId = "body-03",
                TargetHeadingId = "Outcomes",
                PlacementStrategy = BodyLinkPlacementStrategy.AppendToParagraph,
                TargetPath = "/blog/ai-market-research-companies",
                AnchorText = "AI market research companies",
                ContextPhrase = "See our guide to <a href=\"{targetPath}\">{anchorText}</a>.",
                IsTargetActive = true,
            },
        ]);

        Assert.Equal(1, applied);

        Assert.Contains("href=\"/blog/ai-market-research-companies\"", result, StringComparison.Ordinal);
        Assert.Contains("AI market research companies", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyBodyLinks_appends_related_guides_when_h2_hint_does_not_match()
    {
        var (result, applied) = ContentBodyLinkInserter.ApplyBodyLinks(SampleHtml,
        [
            new BodyLinkInsertionInstruction
            {
                LinkId = "body-04",
                TargetHeadingId = "missing-section",
                PlacementStrategy = BodyLinkPlacementStrategy.AppendToParagraph,
                TargetPath = "/blog/ai-market-research-companies",
                AnchorText = "AI market research companies",
                IsTargetActive = true,
            },
        ]);

        Assert.Equal(1, applied);
        Assert.Contains("related-guides", result, StringComparison.Ordinal);
        Assert.Contains("href=\"/blog/ai-market-research-companies\"", result, StringComparison.Ordinal);
    }
}
