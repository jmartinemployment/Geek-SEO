using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerStepValidatorsTests
{
    [Fact]
    public void ValidateSiteIndexStep1_uses_persisted_url_count()
    {
        var site = new SeoSiteResearch
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SiteUrl = "https://example.com",
            DiscoveredUrlsJson = "[\"https://example.com/a\",\"https://example.com/b\"]",
        };

        var result = SiteAnalyzerStepValidators.ValidateSiteIndexStep(1, site);
        Assert.True(result.Passed);
    }

    [Fact]
    public void ValidatePackStep6_fails_without_competitor_headings()
    {
        var research = SiteAnalyzerPackValidatorTests.MinimalComplete();
        research.Competitors =
        [
            new SeoUrlResearchCompetitor
            {
                Id = Guid.NewGuid(),
                UrlResearchId = research.Id,
                Url = "https://a.com",
                Position = 1,
            },
        ];

        var result = SiteAnalyzerStepValidators.ValidatePackStep(6, research);
        Assert.False(result.Passed);
        Assert.Contains("competitors", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePackSteps_passes_partial_quality_when_gates_met()
    {
        var research = SiteAnalyzerPackValidatorTests.MinimalComplete();
        research.DataQuality = "partial";

        var result = SiteAnalyzerStepValidators.ValidatePackSteps(research);
        Assert.True(result.Passed);
    }
}
