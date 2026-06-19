using System.Text.Json;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

/// <summary>Re-validates each Site Analyzer step from persisted artifacts (not step-run rows).</summary>
public static class SiteAnalyzerStepValidators
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static SiteAnalyzerGateResult ValidateSiteIndexStep(int step, SeoSiteResearch site) =>
        step switch
        {
            1 => ValidateSiteStep1(site),
            2 => ValidateSiteStep2(site),
            3 => ValidateSiteStep3(site),
            4 => ValidateSiteStep4(site),
            _ => SiteAnalyzerGateResult.Fail($"Invalid site index step {step}."),
        };

    public static SiteAnalyzerGateResult ValidatePackStep(int step, SeoUrlResearch research) =>
        step switch
        {
            5 => ValidatePackStep5(research),
            6 => ValidatePackStep6(research),
            7 => ValidatePackStep7(research),
            8 => ValidatePackStep8(research),
            9 => ValidatePackStep9(research),
            _ => SiteAnalyzerGateResult.Fail($"Invalid keyword pack step {step}."),
        };

    public static SiteAnalyzerGateResult ValidateSiteIndexSteps(SeoSiteResearch site)
    {
        for (var step = 1; step <= 4; step++)
        {
            var result = ValidateSiteIndexStep(step, site);
            if (!result.Passed)
                return result;
        }

        return SiteAnalyzerGateResult.Pass();
    }

    public static SiteAnalyzerGateResult ValidatePackSteps(SeoUrlResearch research)
    {
        if (research.SiteResearchId is null)
            return SiteAnalyzerGateResult.Fail("Site index not linked — complete Site Analyzer first.");

        for (var step = 5; step <= 9; step++)
        {
            var result = ValidatePackStep(step, research);
            if (!result.Passed)
                return result;
        }

        return SiteAnalyzerGateResult.Pass();
    }

    private static SiteAnalyzerGateResult ValidateSiteStep1(SeoSiteResearch site)
    {
        var urls = ParseUrlList(site.DiscoveredUrlsJson);
        return SiteAnalyzerGates.Step1(urls.Count);
    }

    private static SiteAnalyzerGateResult ValidateSiteStep2(SeoSiteResearch site)
    {
        var pagesFetched = site.Pages.Count(p => !string.IsNullOrWhiteSpace(p.Html));
        return SiteAnalyzerGates.Step2(pagesFetched);
    }

    private static SiteAnalyzerGateResult ValidateSiteStep3(SeoSiteResearch site)
    {
        var crawled = site.Pages.Count(p => !string.IsNullOrWhiteSpace(p.Html));
        var extracted = site.Pages.Count(p => p.ExtractSuccess);
        var failed = site.Pages.Count(p => !string.IsNullOrWhiteSpace(p.Html) && !p.ExtractSuccess);
        return SiteAnalyzerGates.Step3(crawled, extracted, failed);
    }

    private static SiteAnalyzerGateResult ValidateSiteStep4(SeoSiteResearch site)
    {
        var hasSummary = !string.IsNullOrWhiteSpace(site.BusinessSummary);
        var links = ParseJsonArrayLength(site.InternalLinkMapJson);
        return SiteAnalyzerGates.Step4(hasSummary, links > 0);
    }

    private static SiteAnalyzerGateResult ValidatePackStep5(SeoUrlResearch research)
    {
        var organic = research.OrganicResults?.Count ?? 0;
        var paa = research.PeopleAlsoAsk?.Count ?? 0;
        var pasf = research.RelatedSearches?.Count ?? 0;
        var pafOk = HasPafPresentOrExplicitNone(research);
        return SiteAnalyzerGates.Step5(organic, paa, pasf, pafOk);
    }

    private static SiteAnalyzerGateResult ValidatePackStep6(SeoUrlResearch research)
    {
        var competitors = research.Competitors?.Count(c =>
            !string.IsNullOrWhiteSpace(c.H1) || (c.Headings?.Count ?? 0) > 0) ?? 0;
        return SiteAnalyzerGates.Step6(competitors);
    }

    private static SiteAnalyzerGateResult ValidatePackStep7(SeoUrlResearch research)
    {
        var terms = research.RecommendedTerms?.Count ?? 0;
        return SiteAnalyzerGates.Step7(terms);
    }

    private static SiteAnalyzerGateResult ValidatePackStep8(SeoUrlResearch research)
    {
        var hints = research.SectionHints?.Count ?? 0;
        var faqs = research.ClosingFaqs?.Count ?? 0;
        return SiteAnalyzerGates.Step8(hints, faqs);
    }

    private static SiteAnalyzerGateResult ValidatePackStep9(SeoUrlResearch research) =>
        SiteAnalyzerGates.Step9(!string.IsNullOrWhiteSpace(research.BusinessContext));

    private static bool HasPafPresentOrExplicitNone(SeoUrlResearch research)
    {
        if (string.Equals(research.PafType, "none", StringComparison.OrdinalIgnoreCase))
            return true;
        return !string.IsNullOrWhiteSpace(research.PafType)
               && (!string.IsNullOrWhiteSpace(research.PafText)
                   || !string.IsNullOrWhiteSpace(research.PafFormat));
    }

    private static IReadOnlyList<string> ParseUrlList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, Json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int ParseJsonArrayLength(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
