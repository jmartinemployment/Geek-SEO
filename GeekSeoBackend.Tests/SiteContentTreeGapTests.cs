using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;

namespace GeekSeoBackend.Tests;

public sealed class SiteContentTreeGapTests
{
    [Fact]
    public void Homepage_h5_with_real_paragraph_under_pillar_heading_is_a_gap()
    {
        const string html = """
            <h1>Home</h1>
            <h4>AI Content Creation Workflow</h4>
            <p>Overview of the workflow.</p>
            <h5>Marketing</h5>
            <p>Marketing teams run campaigns from this workflow.</p>
            <h5>Sales</h5>
            """;

        var tree = PageSectionTreeBuilder.Build(html);
        var gaps = SiteContentCoverageMatcher.CollectTreeGaps(
            "ai-content-creation-workflow",
            [("https://example.com/", tree)],
            ["https://example.com/", "https://example.com/ai-content-creation-workflow"]);

        Assert.Contains(gaps, g => g.HeadingSlug == "marketing");
        Assert.DoesNotContain(gaps, g => g.HeadingSlug == "sales"); // bare label, no paragraph
        Assert.DoesNotContain(gaps, g => g.HeadingSlug == "ai-content-creation-workflow");
    }

    [Fact]
    public void Bare_heading_with_no_paragraph_is_not_a_gap()
    {
        const string html = "<h2>Services</h2><h3>Plumbing</h3>";
        var tree = PageSectionTreeBuilder.Build(html);

        var gaps = SiteContentCoverageMatcher.CollectTreeGaps(
            "services",
            [("https://example.com/services", tree)],
            ["https://example.com/", "https://example.com/services"]);

        Assert.Empty(gaps);
    }

    [Fact]
    public void Heading_that_already_has_a_dedicated_page_is_not_a_gap()
    {
        const string html = "<h2>Services</h2><h3>Plumbing</h3><p>We fix pipes.</p>";
        var tree = PageSectionTreeBuilder.Build(html);

        var gaps = SiteContentCoverageMatcher.CollectTreeGaps(
            "services",
            [("https://example.com/services", tree)],
            ["https://example.com/", "https://example.com/services", "https://example.com/plumbing"]);

        Assert.Empty(gaps);
    }

    [Fact]
    public void Does_not_manufacture_gaps_from_sitemap_url_segments()
    {
        // A page tree with only a pillar heading (no content-backed children) yields zero gaps
        // even when the sitemap would have produced childSlugs for every URL segment.
        const string html = "<h1>Services</h1><p>Our service catalog.</p>";
        var tree = PageSectionTreeBuilder.Build(html);

        var gaps = SiteContentCoverageMatcher.CollectTreeGaps(
            "services",
            [("https://example.com/services", tree)],
            [
                "https://example.com/",
                "https://example.com/services",
                "https://example.com/services/how-to",
                "https://example.com/services/pricing",
                "https://example.com/services/near-me",
            ]);

        Assert.Empty(gaps);
    }
}
