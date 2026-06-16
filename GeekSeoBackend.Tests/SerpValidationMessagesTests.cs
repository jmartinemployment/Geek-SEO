using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services.NicheExtraction;

namespace GeekSeoBackend.Tests;

public sealed class SerpValidationMessagesTests
{
    [Fact]
    public void Build_EmitsWarningWhenLocalQueriesFail()
    {
        var localStats = new SerpLocalQueryStats("Delray Beach, Florida, United States", 57, 0, 57, "Serper.dev HTTP 429");
        var competitors = new List<NicheCompetitor>
        {
            new() { Domain = "example.com", Scope = "national", SerpPresence = 2 },
        };

        var (summary, warning) = SerpValidationMessages.Build(
            [],
            competitors,
            skipped: false,
            skipReason: null,
            localStats);

        Assert.Contains(SerpValidationMessages.WarningPrefix, summary);
        Assert.NotNull(warning);
        Assert.Contains("57/57", warning);
        Assert.Contains("429", warning);
    }

    [Fact]
    public void TryExtractWarning_ReturnsSubstringAfterPrefix()
    {
        var summary =
            "SERP validation: 10 pillar(s) checked, 5 competitor(s) found. Local SERP issue: Local query failed for 3/10 pillars (Delray Beach, Florida, United States). Serper.dev HTTP 429.";

        var warning = SerpValidationMessages.TryExtractWarning(summary);

        Assert.NotNull(warning);
        Assert.StartsWith(SerpValidationMessages.WarningPrefix, warning);
        Assert.Contains("3/10", warning);
    }

    [Fact]
    public void IsRateLimited_Detects429AndRateLimitText()
    {
        Assert.True(PillarDemandEnricher.IsRateLimited("Serper.dev HTTP 429"));
        Assert.True(PillarDemandEnricher.IsRateLimited("Provider rate limit exceeded"));
        Assert.False(PillarDemandEnricher.IsRateLimited("timeout"));
    }

    [Fact]
    public void TryExtractWarning_SurfacesLegacy429SkippedSummary()
    {
        var warning = SerpValidationMessages.TryExtractWarning(
            "SERP validation skipped — Serper.dev HTTP 429 (rate limit).");

        Assert.NotNull(warning);
        Assert.StartsWith(SerpValidationMessages.WarningPrefix, warning);
        Assert.Contains("429", warning);
    }
}
