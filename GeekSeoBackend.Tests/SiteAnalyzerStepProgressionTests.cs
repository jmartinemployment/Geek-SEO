using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerStepProgressionTests
{
    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(8, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    public void PackPersistSteps_only_includes_validated_write_steps(int step, bool expected)
    {
        Assert.Equal(expected, SiteAnalyzerStepProgression.IsPackPersistStep(step));
    }

    [Fact]
    public void PriorStepsGreen_blocks_when_immediate_prior_step_is_red()
    {
        var runs = new List<SiteAnalyzerStepRunRow>
        {
            new() { StepNumber = 5, Status = "green" },
            new() { StepNumber = 6, Status = "red", Message = "Step 6: at least 3 competitors required; got 1" },
        };

        Assert.False(SiteAnalyzerStepProgression.PriorStepsGreen(7, runs, minStep: 5));
    }

    [Fact]
    public void PriorStepsGreen_allows_next_step_when_all_prior_steps_are_green()
    {
        var runs = new List<SiteAnalyzerStepRunRow>
        {
            new() { StepNumber = 5, Status = "green" },
            new() { StepNumber = 6, Status = "green" },
            new() { StepNumber = 7, Status = "green" },
            new() { StepNumber = 8, Status = "green" },
            new() { StepNumber = 9, Status = "green" },
        };

        Assert.True(SiteAnalyzerStepProgression.PriorStepsGreen(10, runs, minStep: 5));
    }

    [Fact]
    public void PriorStepsGreen_blocks_step_10_when_step_9_is_not_green()
    {
        var runs = new List<SiteAnalyzerStepRunRow>
        {
            new() { StepNumber = 5, Status = "green" },
            new() { StepNumber = 6, Status = "green" },
            new() { StepNumber = 7, Status = "green" },
            new() { StepNumber = 8, Status = "green" },
            new() { StepNumber = 9, Status = "red", Message = "Step 9: site context merge incomplete" },
        };

        Assert.False(SiteAnalyzerStepProgression.PriorStepsGreen(10, runs, minStep: 5));
    }
}
