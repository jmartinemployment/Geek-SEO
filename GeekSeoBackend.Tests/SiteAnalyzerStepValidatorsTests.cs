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

    [Fact]
    public void ValidatePackStepFromBuild_fails_before_persist_when_paa_missing()
    {
        var pack = SiteAnalyzerPackValidatorTests.MinimalSerpResearchPack() with
        {
            Paa = [],
        };

        var result = SiteAnalyzerStepValidators.ValidatePackStepFromBuild(5, pack);
        Assert.False(result.Passed);
        Assert.Contains("PAA", result.Message);
        Assert.StartsWith("Step 5:", result.Message);
    }

    [Theory]
    [InlineData(1, "Step 1:")]
    [InlineData(2, "Step 2:")]
    [InlineData(3, "Step 3:")]
    [InlineData(4, "Step 4:")]
    [InlineData(5, "Step 5:")]
    [InlineData(6, "Step 6:")]
    [InlineData(7, "Step 7:")]
    [InlineData(8, "Step 8:")]
    [InlineData(9, "Step 9:")]
    [InlineData(10, "Step 10:")]
    public void Each_gate_failure_message_is_step_scoped(int step, string prefix)
    {
        var result = step switch
        {
            1 => SiteAnalyzerGates.Step1(0),
            2 => SiteAnalyzerGates.Step2(0),
            3 => SiteAnalyzerGates.Step3(1, 0, 1),
            4 => SiteAnalyzerGates.Step4(false, false),
            5 => SiteAnalyzerGates.Step5(0, 0, 0, false),
            6 => SiteAnalyzerGates.Step6(0),
            7 => SiteAnalyzerGates.Step7(0),
            8 => SiteAnalyzerGates.Step8(0, 0),
            9 => SiteAnalyzerGates.Step9(false),
            10 => SiteAnalyzerGates.Step10(false),
            _ => throw new ArgumentOutOfRangeException(nameof(step)),
        };

        Assert.False(result.Passed);
        Assert.StartsWith(prefix, result.Message);
    }
}
