using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalysisStepCatalogTests
{
    [Fact]
    public void Ordered_ExposesCanonicalNineStepSequence()
    {
        // Steps 9-16 of the old pipeline (merging/keywords/serp_validation/profile/local/
        // coverage/scoring) were topic-candidate/pillar/evidence machinery with no live
        // consumer left after Content Creator moved to mechanical heading-vs-page gap
        // detection (SiteContentCoverageMatcher.CollectAllHeadingGaps). Only the raw
        // crawl/discovery steps plus a terminal marker remain.
        var ordered = SiteAnalysisStepCatalog.Ordered;

        Assert.Equal(9, ordered.Count);
        Assert.Equal(
            [
                "schema",
                "site_urls",
                "nav",
                "headings",
                "page_content",
                "site_crawl",
                "internal_links",
                "url_patterns",
                "complete",
            ],
            ordered.Select(step => step.Slug).ToArray());
    }

    [Fact]
    public void GetDownstream_FromSiteCrawl_IncludesStructureAndTerminalStep()
    {
        var downstream = SiteAnalysisStepCatalog.GetDownstream("site_crawl");

        Assert.Equal(
            ["internal_links", "url_patterns", "complete"],
            downstream);
    }

    [Fact]
    public void ToDtos_MatchesCanonicalMetadata()
    {
        var dtos = SiteAnalysisStepCatalog.ToDtos();
        var terminal = dtos.Single(step => step.Slug == "complete");

        Assert.Equal(SiteAnalysisStepCatalog.Ordered.Count, dtos.Count);
        Assert.True(terminal.IsTerminal);
        Assert.False(terminal.IsOptional);
    }
}
