namespace GeekSeo.Application.Services.Seo;

/// <summary>Fixed v1 gate minimums — serviceable market definition.</summary>
public static class SiteAnalyzerGates
{
    public const int MinSitemapUrls = 1;
    public const int MinPagesCrawled = 1;
    public const int MinOrganic = 1;
    public const int MinPaa = 1;
    public const int MinPasf = 1;
    public const int MinCompetitors = 3;
    public const int MinTerms = 8;
    public const int MinSectionHints = 4;
    public const int RequiredClosingFaqs = 5;
    public const int MaxCrawlPages = 50;

    public static SiteAnalyzerGateResult Step1(int urlCount) =>
        urlCount >= MinSitemapUrls
            ? SiteAnalyzerGateResult.Pass()
            : SiteAnalyzerGateResult.Fail($"Step 1: at least {MinSitemapUrls} URL required; got {urlCount}");

    public static SiteAnalyzerGateResult Step2(int pagesCrawled) =>
        pagesCrawled >= MinPagesCrawled
            ? SiteAnalyzerGateResult.Pass()
            : SiteAnalyzerGateResult.Fail($"Step 2: at least {MinPagesCrawled} page crawled; got {pagesCrawled}");

    public static SiteAnalyzerGateResult Step3(int crawledCount, int extractedCount, int failedCount) =>
        failedCount == 0 && extractedCount == crawledCount && crawledCount > 0
            ? SiteAnalyzerGateResult.Pass()
            : SiteAnalyzerGateResult.Fail($"Step 3: extraction failed on {failedCount} of {crawledCount} pages");

    public static SiteAnalyzerGateResult Step4(bool hasSummary, bool hasLinkMap) =>
        hasSummary && hasLinkMap
            ? SiteAnalyzerGateResult.Pass()
            : !hasSummary && !hasLinkMap
                ? SiteAnalyzerGateResult.Fail("Step 4: site summary and internal link map missing")
                : !hasSummary
                    ? SiteAnalyzerGateResult.Fail("Step 4: site summary missing")
                    : SiteAnalyzerGateResult.Fail("Step 4: internal link map missing");

    public static SiteAnalyzerGateResult Step5(int organic, int paa, int pasf, bool pafPresentOrExplicitNone) =>
        organic < MinOrganic
            ? SiteAnalyzerGateResult.Fail($"Step 5: at least {MinOrganic} organic result required; got {organic}")
            : paa < MinPaa
                ? SiteAnalyzerGateResult.Fail($"Step 5: at least {MinPaa} PAA required; got {paa}")
                : pasf < MinPasf
                    ? SiteAnalyzerGateResult.Fail($"Step 5: at least {MinPasf} PASF required; got {pasf}")
                    : !pafPresentOrExplicitNone
                        ? SiteAnalyzerGateResult.Fail("Step 5: PAF missing — record results or explicit none")
                        : SiteAnalyzerGateResult.Pass();

    public static SiteAnalyzerGateResult Step6(int competitors) =>
        competitors >= MinCompetitors
            ? SiteAnalyzerGateResult.Pass()
            : SiteAnalyzerGateResult.Fail($"Step 6: at least {MinCompetitors} competitors required; got {competitors}");

    public static SiteAnalyzerGateResult Step7(int terms) =>
        terms >= MinTerms
            ? SiteAnalyzerGateResult.Pass()
            : SiteAnalyzerGateResult.Fail($"Step 7: at least {MinTerms} terms required; got {terms}");

    public static SiteAnalyzerGateResult Step8(int sectionHints, int closingFaqs) =>
        sectionHints < MinSectionHints
            ? SiteAnalyzerGateResult.Fail($"Step 8: at least {MinSectionHints} section hints required; got {sectionHints}")
            : closingFaqs != RequiredClosingFaqs
                ? SiteAnalyzerGateResult.Fail($"Step 8: {RequiredClosingFaqs} closing FAQs required; got {closingFaqs}")
                : SiteAnalyzerGateResult.Pass();

    public static SiteAnalyzerGateResult Step9(bool mergeComplete) =>
        mergeComplete
            ? SiteAnalyzerGateResult.Pass()
            : SiteAnalyzerGateResult.Fail("Step 9: site context merge incomplete");

    public static SiteAnalyzerGateResult Step10(bool handoffReady) =>
        handoffReady
            ? SiteAnalyzerGateResult.Pass()
            : SiteAnalyzerGateResult.Fail("Step 10: pack not finalized — complete steps 5–9 first, then run this step.");
}

public sealed record SiteAnalyzerGateResult(bool Passed, string Message)
{
    public static SiteAnalyzerGateResult Pass() => new(true, string.Empty);
    public static SiteAnalyzerGateResult Fail(string message) => new(false, message);
}
