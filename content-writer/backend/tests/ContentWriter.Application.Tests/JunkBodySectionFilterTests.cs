using ContentWriter.Application.Providers;
using ContentWriter.Application.Services.PromptBuilders;
using ContentWriter.Application.Services.Publish;

namespace ContentWriter.Application.Tests;

public class JunkBodySectionFilterTests
{
    [Theory]
    [InlineData("Related Items", true)]
    [InlineData("Related Articles", true)]
    [InlineData("Further reading", true)]
    [InlineData("What to read next", true)]
    [InlineData("People Also Ask", false)]
    [InlineData("Top AI Tools for Sales", false)]
    public void IsJunkSectionHeading_classifies_related_sections(string heading, bool expected) =>
        Assert.Equal(expected, JunkBodySectionFilter.IsJunkSectionHeading(heading));

    [Fact]
    public void StripJunkSectionsFromHtml_removes_related_items_h2_block()
    {
        var html = """
            <h2>Overview</h2><p>Main content.</p>
            <h2>Related Items</h2><ul><li><a href="/blog/x">Blog</a></li></ul>
            <h2>People Also Ask</h2><h3>Question?</h3><p>Answer.</p>
            """;

        var stripped = JunkBodySectionFilter.StripJunkSectionsFromHtml(html);

        Assert.Contains("Overview", stripped);
        Assert.Contains("People Also Ask", stripped);
        Assert.DoesNotContain("Related Items", stripped);
    }

    [Fact]
    public void ValidateMarkdownHasNoJunkSections_rejects_related_items_heading()
    {
        var markdown = """
            ## Overview
            Body text.

            ## Related Items
            - [Blog](/blog/example)
            """;

        var ex = Assert.Throws<ContentGenerationException>(
            () => JunkBodySectionFilter.ValidateMarkdownHasNoJunkSections(markdown));

        Assert.Contains("Related Items", ex.Message);
    }

    [Fact]
    public void PillarOutlineNormalizer_drops_related_items_from_outline()
    {
        var (main, _) = PillarOutlineNormalizer.Sanitize(
            ["Overview", "Related Items", "People Also Ask"],
            [],
            "expense management");

        Assert.DoesNotContain(main, h => JunkBodySectionFilter.IsJunkSectionHeading(h));
        Assert.Contains(main, h => h.Equals("Overview", StringComparison.Ordinal));
        Assert.Contains(main, h => h.Equals("People Also Ask", StringComparison.Ordinal));
    }
}
