using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public static class SiteAnalyzerPackValidator
{
    /// <summary>Steps 5–9 minimums on persisted pack data. Does not require <c>data_quality = full</c>.</summary>
    public static SiteAnalyzerGateResult ValidateGateMinimums(SeoUrlResearch research) =>
        SiteAnalyzerStepValidators.ValidatePackSteps(research);

    /// <summary>Content Writing handoff — finalized pack with <c>data_quality = full</c>.</summary>
    public static SiteAnalyzerGateResult ValidateCompletePack(SeoUrlResearch research)
    {
        if (!string.Equals(research.DataQuality, "full", StringComparison.OrdinalIgnoreCase))
            return SiteAnalyzerGateResult.Fail("Research pack is not finalized (data_quality must be full).");

        return ValidateGateMinimums(research);
    }

    public static bool IsHandoffReady(SeoUrlResearch research) =>
        string.Equals(research.Status, "completed", StringComparison.OrdinalIgnoreCase)
        && ValidateCompletePack(research).Passed;
}
