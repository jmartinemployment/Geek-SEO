using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public static class SiteAnalyzerPackValidator
{
    public static SiteAnalyzerGateResult ValidateCompletePack(SeoUrlResearch research)
    {
        if (research.SiteResearchId is null)
            return SiteAnalyzerGateResult.Fail("Site index not linked — complete Site Analyzer first.");

        if (!string.Equals(research.DataQuality, "full", StringComparison.OrdinalIgnoreCase))
            return SiteAnalyzerGateResult.Fail("Research pack is not finalized (data_quality must be full).");

        var organic = research.OrganicResults?.Count ?? 0;
        var paa = research.PeopleAlsoAsk?.Count ?? 0;
        var pasf = research.RelatedSearches?.Count ?? 0;
        var pafOk = HasPafPresentOrExplicitNone(research);
        var step5 = SiteAnalyzerGates.Step5(organic, paa, pasf, pafOk);
        if (!step5.Passed)
            return step5;

        var competitors = research.Competitors?.Count(c =>
            !string.IsNullOrWhiteSpace(c.H1) || (c.Headings?.Count ?? 0) > 0) ?? 0;
        var step6 = SiteAnalyzerGates.Step6(competitors);
        if (!step6.Passed)
            return step6;

        var terms = research.RecommendedTerms?.Count ?? 0;
        var step7 = SiteAnalyzerGates.Step7(terms);
        if (!step7.Passed)
            return step7;

        var hints = research.SectionHints?.Count ?? 0;
        var faqs = research.ClosingFaqs?.Count ?? 0;
        var step8 = SiteAnalyzerGates.Step8(hints, faqs);
        if (!step8.Passed)
            return step8;

        if (string.IsNullOrWhiteSpace(research.BusinessContext))
            return SiteAnalyzerGates.Step9(false);

        return SiteAnalyzerGateResult.Pass();
    }

    public static bool IsHandoffReady(SeoUrlResearch research) =>
        string.Equals(research.Status, "completed", StringComparison.OrdinalIgnoreCase)
        && ValidateCompletePack(research).Passed;

    private static bool HasPafPresentOrExplicitNone(SeoUrlResearch research)
    {
        if (string.Equals(research.PafType, "none", StringComparison.OrdinalIgnoreCase))
            return true;
        return !string.IsNullOrWhiteSpace(research.PafType)
               && (!string.IsNullOrWhiteSpace(research.PafText)
                   || !string.IsNullOrWhiteSpace(research.PafFormat));
    }
}
