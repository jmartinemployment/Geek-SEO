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
    public void Headings_inside_anchors_are_still_headings()
    {
        const string html = """
            <h2>Artificial Intelligence Use Cases</h2>
            <h3>Accounting</h3>
            <a href="/use-cases/accounting/accounts-payable/automated-accounts-payable">
              <h4>Automated Accounts Payable</h4>
            </a>
            <a href="/use-cases/marketing/ai-marketing-systems">
              <h3>Marketing</h3>
            </a>
            <h4>AI Content Creation Workflow</h4>
            """;

        var tree = PageSectionTreeBuilder.Build(html);

        var h2 = Assert.Single(tree);
        Assert.Equal("Artificial Intelligence Use Cases", h2.HeadingText);
        Assert.Equal(2, h2.Children.Count);
        Assert.Equal("Accounting", h2.Children[0].HeadingText);
        Assert.Equal("Automated Accounts Payable", Assert.Single(h2.Children[0].Children).HeadingText);
        Assert.Equal("Marketing", h2.Children[1].HeadingText);
        Assert.Equal("AI Content Creation Workflow", Assert.Single(h2.Children[1].Children).HeadingText);
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

    [Fact]
    public void Br_and_block_boundaries_are_word_separators()
    {
        const string html = "<h2>Clone Yourself<br/><span>Work 24/7</span></h2>";
        var tree = PageSectionTreeBuilder.Build(html);
        var h2 = Assert.Single(tree);
        Assert.Equal("Clone Yourself Work 24/7", h2.HeadingText);
    }

    [Fact]
    public void Aria_hidden_collapsed_sizer_wins_over_partial_visible_sibling()
    {
        const string html =
            """<h1>Redefine Your Business<br/><span aria-hidden="true" data-gsv="collapsed">Efficiency</span><span data-gsv="visible">Effici</span></h1>""";
        var tree = PageSectionTreeBuilder.Build(html);
        var h1 = Assert.Single(tree);
        Assert.Equal("Redefine Your Business Efficiency", h1.HeadingText);
    }

    [Fact]
    public void Desktop_only_subtree_contributes_no_text_but_links_are_kept()
    {
        const string html = """
            <h1>Home</h1>
            <div data-gsv="desktop-only">
              <p>Desktop hero copy that Google does not index on mobile.</p>
              <a href="/desktop-only-link">Hidden nav</a>
            </div>
            <p>Mobile body.</p>
            """;

        var tree = PageSectionTreeBuilder.Build(html);
        var h1 = Assert.Single(tree);
        Assert.Equal(["Mobile body."], h1.Paragraphs);
        Assert.Contains(h1.Links, l => l.Href == "/desktop-only-link");
    }

    [Fact]
    public void Collapsed_subtree_contributes_text()
    {
        const string html = """
            <h2>FAQ</h2>
            <div data-gsv="collapsed"><p>Accordion answer Google indexes at full weight.</p></div>
            """;

        var tree = PageSectionTreeBuilder.Build(html);
        var h2 = Assert.Single(tree);
        Assert.Contains("Accordion answer Google indexes at full weight.", h2.Paragraphs);
    }

    [Fact]
    public void Script_and_style_inside_heading_contribute_no_text()
    {
        const string html = "<h2>Title<script>var x=1</script><style>.x{}</style></h2>";
        var tree = PageSectionTreeBuilder.Build(html);
        var h2 = Assert.Single(tree);
        Assert.Equal("Title", h2.HeadingText);
    }

    [Fact]
    public void H5_inside_li_is_a_heading_not_swallowed_by_the_list_item()
    {
        const string html = """
            <h4>AI Content Creation Workflow</h4>
            <ul>
              <li><h5>Automated Content Generation</h5></li>
              <li><h5>AI Content Repurposing</h5></li>
            </ul>
            """;

        var tree = PageSectionTreeBuilder.Build(html);
        var h4 = Assert.Single(tree);
        Assert.Equal(2, h4.Children.Count);
        Assert.Equal("Automated Content Generation", h4.Children[0].HeadingText);
        Assert.Equal("AI Content Repurposing", h4.Children[1].HeadingText);
    }

    [Fact]
    public void Harvests_anchors_from_list_items_not_only_paragraphs()
    {
        const string html = """
            <h6>Top 5 Automated Data Entry Processing Tools:</h6>
            <ul>
              <li><a href="/tools/zapier">Zapier</a></li>
              <li><a href="/tools/quickbooks">QuickBooks</a></li>
            </ul>
            """;

        var tree = PageSectionTreeBuilder.Build(html);
        var h6 = Assert.Single(tree);
        Assert.Equal(2, h6.Links.Count);
        Assert.Contains(h6.Links, l => l.Href == "/tools/zapier" && l.Text == "Zapier");
    }
}
