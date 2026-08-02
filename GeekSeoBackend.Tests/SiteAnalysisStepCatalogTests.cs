using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalysisStepCatalogTests
{
    [Fact]
    public void Ordered_ExposesCanonicalSixteenStepSequence()
    {
        var ordered = SiteAnalysisStepCatalog.Ordered;

        Assert.Equal(16, ordered.Count);
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
                "merging",
                "keywords",
                "serp_validation",
                "profile",
                "local",
                "coverage",
                "scoring",
                "complete",
            ],
            ordered.Select(step => step.Slug).ToArray());
    }

    [Fact]
    public void GetDownstream_ReturnsTransitiveDependentsInStepOrder()
    {
        var downstream = SiteAnalysisStepCatalog.GetDownstream("merging");

        Assert.Equal(
            ["keywords", "serp_validation", "profile", "local", "coverage", "scoring", "complete"],
            downstream);
    }

    [Fact]
    public void GetDownstream_FromSiteCrawl_IncludesStructureAndMergeSteps()
    {
        var downstream = SiteAnalysisStepCatalog.GetDownstream("site_crawl");

        Assert.Equal(
            ["internal_links", "url_patterns", "merging", "keywords", "serp_validation", "profile", "local", "coverage", "scoring", "complete"],
            downstream);
    }

    [Fact]
    public void ToDtos_MatchesCanonicalMetadata()
    {
        var dtos = SiteAnalysisStepCatalog.ToDtos();
        var validate = dtos.Single(step => step.Slug == "keywords");
        var terminal = dtos.Single(step => step.Slug == "complete");

        Assert.Equal(SiteAnalysisStepCatalog.Ordered.Count, dtos.Count);
        Assert.Equal("validate", validate.Phase);
        Assert.True(validate.IsOptional);
        Assert.False(validate.IsTerminal);
        Assert.Equal(["merging"], validate.Dependencies);
        Assert.True(terminal.IsTerminal);
        Assert.False(terminal.IsOptional);
    }
}
