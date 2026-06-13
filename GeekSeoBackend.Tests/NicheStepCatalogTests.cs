using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.NicheStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class NicheStepCatalogTests
{
    [Fact]
    public void Ordered_ExposesCanonicalFourteenStepSequence()
    {
        var ordered = NicheStepCatalog.Ordered;

        Assert.Equal(14, ordered.Count);
        Assert.Equal(
            [
                "schema",
                "site_urls",
                "nav",
                "headings",
                "page_content",
                "site_structure",
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
        var downstream = NicheStepCatalog.GetDownstream("merging");

        Assert.Equal(
            ["keywords", "serp_validation", "profile", "local", "coverage", "scoring", "complete"],
            downstream);
    }

    [Fact]
    public void ToDtos_MatchesCanonicalMetadata()
    {
        var dtos = NicheStepCatalog.ToDtos();
        var validate = dtos.Single(step => step.Slug == "keywords");
        var terminal = dtos.Single(step => step.Slug == "complete");

        Assert.Equal(NicheStepCatalog.Ordered.Count, dtos.Count);
        Assert.Equal("validate", validate.Phase);
        Assert.True(validate.IsOptional);
        Assert.False(validate.IsTerminal);
        Assert.Equal(["merging"], validate.Dependencies);
        Assert.True(terminal.IsTerminal);
        Assert.False(terminal.IsOptional);
    }
}
