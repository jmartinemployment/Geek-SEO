using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class PageSectionTreeBuilderTests
{
    [Fact]
    public void Nests_multi_level_headings_and_assigns_paragraphs_to_nearest_preceding_heading()
    {
        const string html = """
            <html><body>
              <h1>AI Content Creation Workflow</h1>
              <p>An overview paragraph about the workflow.</p>
              <h2>Marketing</h2>
              <p>Marketing teams use this workflow to plan campaigns.</p>
              <h3>Social Media</h3>
              <p>Social posts are generated from the same brief.</p>
              <h2>Sales</h2>
              <p>Sales enablement content reuses the same pillar.</p>
            </body></html>
            """;

        var tree = PageSectionTreeBuilder.Build(html);

        var h1 = Assert.Single(tree);
        Assert.Equal(1, h1.Level);
        Assert.Equal("AI Content Creation Workflow", h1.HeadingText);
        Assert.Equal(["An overview paragraph about the workflow."], h1.Paragraphs);
        Assert.Equal(2, h1.Children.Count);

        var marketing = h1.Children[0];
        Assert.Equal("Marketing", marketing.HeadingText);
        Assert.Equal(["Marketing teams use this workflow to plan campaigns."], marketing.Paragraphs);
        var social = Assert.Single(marketing.Children);
        Assert.Equal(3, social.Level);
        Assert.Equal("Social Media", social.HeadingText);
        Assert.Equal(["Social posts are generated from the same brief."], social.Paragraphs);

        var sales = h1.Children[1];
        Assert.Equal("Sales", sales.HeadingText);
        Assert.Empty(sales.Children);
    }

    [Fact]
    public void Heading_with_real_paragraph_text_has_own_content()
    {
        const string html = "<h2>Marketing</h2><h3>Content Strategy</h3><p>Real body copy here.</p>";

        var tree = PageSectionTreeBuilder.Build(html);

        var marketing = Assert.Single(tree);
        Assert.False(marketing.HasOwnContent);
        var contentStrategy = Assert.Single(marketing.Children);
        Assert.True(contentStrategy.HasOwnContent);
    }

    [Fact]
    public void Bare_heading_with_no_paragraphs_has_no_own_content()
    {
        const string html = "<h4>AI Content Creation Workflow</h4><h5>Marketing</h5>";

        var tree = PageSectionTreeBuilder.Build(html);

        var h4 = Assert.Single(tree);
        Assert.False(h4.HasOwnContent);
        var h5 = Assert.Single(h4.Children);
        Assert.False(h5.HasOwnContent);
        Assert.Empty(h5.Paragraphs);
    }

    [Fact]
    public void Paragraph_text_before_any_heading_is_dropped_not_attached_anywhere()
    {
        const string html = "<p>Intro copy before any heading.</p><h1>Title</h1><p>Body under title.</p>";

        var tree = PageSectionTreeBuilder.Build(html);

        var title = Assert.Single(tree);
        Assert.Equal(["Body under title."], title.Paragraphs);
    }

    [Fact]
    public void Sibling_heading_at_same_level_closes_prior_sections_own_scope()
    {
        const string html = "<h2>A</h2><h3>A1</h3><h2>B</h2>";

        var tree = PageSectionTreeBuilder.Build(html);

        Assert.Equal(2, tree.Count);
        Assert.Equal("A", tree[0].HeadingText);
        Assert.Single(tree[0].Children);
        Assert.Equal("B", tree[1].HeadingText);
        Assert.Empty(tree[1].Children);
    }
}
