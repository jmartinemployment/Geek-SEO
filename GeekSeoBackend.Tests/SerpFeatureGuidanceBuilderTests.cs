using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SerpFeatureGuidanceBuilderTests
{
    [Fact]
    public void Build_ai_overview_uses_concise_definition_guidance_only()
    {
        var guidance = SerpFeatureGuidanceBuilder.Build(new SerpFeatures { HasAiOverview = true });

        var aiOverview = Assert.Single(guidance);
        Assert.Equal("ai_overview", aiOverview.Feature);
        Assert.Equal(SerpFeatureGuidanceBuilder.AiOverviewInsightActionText, aiOverview.ActionText);
        Assert.Equal("serp_ai_overview", aiOverview.SuggestionId);
        Assert.Equal("deterministic", aiOverview.ApplyMode);
        Assert.DoesNotContain("authoritative sources", aiOverview.ActionText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("extractable answers", aiOverview.ActionText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAiOverviewDraftInstruction_targets_opening_paragraph()
    {
        var instruction = SerpFeatureGuidanceBuilder.BuildAiOverviewDraftInstruction("widget repair");

        Assert.Contains("widget repair", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lead with a concise definition", instruction, StringComparison.OrdinalIgnoreCase);
    }
}
