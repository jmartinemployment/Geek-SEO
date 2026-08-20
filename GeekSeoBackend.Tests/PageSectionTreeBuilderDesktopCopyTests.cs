using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

/// <summary>
/// Mobile only. The desktop copy of <c>article#use-cases-section</c> is hidden at the mobile
/// viewport, so the crawler marks it and the builder prunes the whole subtree. None of its
/// headings, links, or paragraphs may reach the tree — otherwise every section on this site
/// appears twice and the hierarchy match has two candidates for one piece of content.
/// </summary>
public class PageSectionTreeBuilderDesktopCopyTests
{
    private static string FixtureHtml()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "geekatyourspot-use-cases-desktop.annotated.html");
        return File.ReadAllText(path);
    }

    [Fact]
    public void Hidden_at_mobile_copy_is_pruned_entirely()
    {
        var tree = PageSectionTreeBuilder.Build(FixtureHtml());
        Assert.Empty(tree);
    }

    [Fact]
    public void Hidden_at_mobile_copy_contributes_no_tool_links()
    {
        var html = FixtureHtml();
        // The fixture really does contain the links — proving the prune, not an empty input.
        Assert.Contains("/tools/marketing/omneky", html, StringComparison.OrdinalIgnoreCase);

        var tree = PageSectionTreeBuilder.Build(html);
        Assert.Empty(tree);
    }
}
