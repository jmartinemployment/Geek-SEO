using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.Publish;

namespace ContentWriter.Application.Tests;

public class BodyPreambleFilterTests
{
    [Fact]
    public void StripPreambleFromHtml_removes_paragraphs_before_first_h2()
    {
        var html = """
            <p>Intro paragraph one.</p>
            <p>Intro paragraph two.</p>
            <h2>Overview</h2><p>Section body.</p>
            """;

        var stripped = BodyPreambleFilter.StripPreambleFromHtml(html);

        Assert.StartsWith("<h2>", stripped, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Intro paragraph", stripped);
        Assert.Contains("Overview", stripped);
    }

    [Fact]
    public void ValidateMarkdownStartsWithSection_rejects_preamble_before_first_heading()
    {
        var markdown = """
            Lead paragraph before sections.

            ## Overview
            Body text.
            """;

        var ex = Assert.Throws<ContentGenerationException>(
            () => BodyPreambleFilter.ValidateMarkdownStartsWithSection(markdown));

        Assert.Contains("first ## section", ex.Message);
    }

    [Fact]
    public void ValidateMarkdownStartsWithSection_allows_body_starting_at_h2()
    {
        var markdown = """
            ## Overview
            Body text.
            """;

        BodyPreambleFilter.ValidateMarkdownStartsWithSection(markdown);
    }

    [Fact]
    public void GeneratedBodyHtmlNormalizer_strips_preamble_and_related_items()
    {
        var html = """
            <p>Preamble.</p>
            <h2>Overview</h2><p>Main.</p>
            <h2>Related Items</h2><p>Links.</p>
            <h2>People Also Ask</h2><h3>Q?</h3><p>A.</p>
            """;

        var normalized = JunkBodySectionFilter.StripJunkSectionsFromHtml(
            BodyPreambleFilter.StripPreambleFromHtml(html));

        Assert.StartsWith("<h2>", normalized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Preamble", normalized);
        Assert.DoesNotContain("Related Items", normalized);
        Assert.Contains("People Also Ask", normalized);
    }
}
