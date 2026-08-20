using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

/// <summary>
/// Regression guard against a real captured page. Fixture is the mobile copy of
/// <c>article#use-cases-section</c> from geekatyourspot.com, rendered at Pixel 7 and annotated with
/// <c>data-gsv</c> exactly as <see cref="SitePageCrawler"/> does.
/// <para>
/// Ground truth verified 2026-08-20 against the live page: the h4 "Automated Ad Spend Optimization"
/// has 4 h5 children and 17 unique <c>/tools/</c> links beneath it. Before the desktop-only /
/// heading-drop fix, the harvest reported 0 links there.
/// </para>
/// </summary>
public class PageSectionTreeBuilderRealPageTests
{
    private const string TargetHeading = "Automated Ad Spend Optimization";

    private static IReadOnlyList<PageSection> BuildFixture()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "geekatyourspot-use-cases-mobile.annotated.html");
        return PageSectionTreeBuilder.Build(File.ReadAllText(path));
    }

    private static IEnumerable<PageSection> Flatten(IEnumerable<PageSection> nodes)
    {
        foreach (var n in nodes)
        {
            yield return n;
            foreach (var c in Flatten(n.Children))
                yield return c;
        }
    }

    private static PageSection FindTarget()
    {
        var match = Flatten(BuildFixture())
            .FirstOrDefault(n => n.HeadingText.Contains(TargetHeading, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(match);
        return match!;
    }

    [Fact]
    public void Ad_spend_section_is_an_h4()
    {
        Assert.Equal(4, FindTarget().Level);
    }

    [Fact]
    public void Ad_spend_section_has_four_h5_children()
    {
        var h5s = Flatten([FindTarget()]).Where(n => n.Level == 5).ToList();
        Assert.Equal(4, h5s.Count);
    }

    [Fact]
    public void Ad_spend_section_yields_seventeen_unique_tool_links()
    {
        var hrefs = Flatten([FindTarget()])
            .SelectMany(n => n.Links)
            .Select(l => l.Href)
            .Where(h => !string.IsNullOrWhiteSpace(h) && h.Contains("/tools/", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(17, hrefs.Count);
    }

    [Fact]
    public void No_heading_in_the_real_page_is_silently_dropped()
    {
        // Every heading node must carry text. An empty one means extraction failed; the builder
        // now keeps such nodes rather than reparenting their children, so they are visible here.
        var empty = Flatten(BuildFixture())
            .Where(n => string.IsNullOrWhiteSpace(n.HeadingText))
            .ToList();

        Assert.Empty(empty);
    }
}
