using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerGatesTests
{
    [Fact]
    public void Step5_fails_when_paa_missing()
    {
        var gate = SiteAnalyzerGates.Step5(organic: 3, paa: 0, pasf: 2, pafPresentOrExplicitNone: true);
        Assert.False(gate.Passed);
        Assert.Contains("PAA", gate.Message);
    }

    [Fact]
    public void Step8_requires_exactly_five_faqs()
    {
        var gate = SiteAnalyzerGates.Step8(sectionHints: 4, closingFaqs: 4);
        Assert.False(gate.Passed);
        Assert.Contains("5 closing FAQs", gate.Message);
    }

    [Fact]
    public void Step3_fails_on_any_extract_failure()
    {
        var gate = SiteAnalyzerGates.Step3(crawledCount: 5, extractedCount: 4, failedCount: 1);
        Assert.False(gate.Passed);
        Assert.Contains("extraction failed", gate.Message);
    }
}
