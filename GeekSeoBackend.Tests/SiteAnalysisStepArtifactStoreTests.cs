using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.SiteExtraction;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class SiteAnalyzerStepArtifactStoreTests
{
    private sealed record SampleArtifact(string Value, int Count);

    [Fact]
    public void WithArtifact_StoresRoundTrippablePayload()
    {
        var entry = new SiteAnalysisStepLogEntry(
            7,
            "merging",
            "Topic selection",
            "complete",
            "saved",
            new Dictionary<string, object?>());

        var stored = SiteAnalyzerStepArtifactStore.WithArtifact(
            entry,
            "sample",
            new SampleArtifact("topic-pool", 3));

        var artifact = SiteAnalyzerStepArtifactStore.GetRequiredArtifact<SampleArtifact>(
            [stored],
            "merging",
            "sample");

        Assert.Equal("topic-pool", artifact.Value);
        Assert.Equal(3, artifact.Count);
    }

    [Fact]
    public void TryGetArtifact_ReturnsNullWhenArtifactTypeDoesNotMatch()
    {
        var entry = SiteAnalyzerStepArtifactStore.WithArtifact(
            new SiteAnalysisStepLogEntry(
                8,
                "keywords",
                "Keyword demand",
                "complete",
                "saved",
                new Dictionary<string, object?>()),
            "keywords",
            new SampleArtifact("demand", 1));

        var artifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<SampleArtifact>(
            [entry],
            "keywords",
            "serp_validation");

        Assert.Null(artifact);
    }

    [Fact]
    public void WithArtifact_StripsCrawlHtmlBeforePersistence()
    {
        var crawl = new SiteCrawlData(
            [new CrawledPage("https://example.com/", new string('x', 50_000), "http")],
            1,
            1);
        var artifact = new SiteAnalyzerStepArtifactStore.SiteStructureArtifact(
            crawl,
            new InternalLinkData([], new Dictionary<string, int>(), 1),
            new UrlPatternData([], 0),
            ["https://example.com/"]);

        var entry = SiteAnalyzerStepArtifactStore.WithArtifact(
            new SiteAnalysisStepLogEntry(
                6,
                "site_crawl",
                "Site crawl",
                "complete",
                "saved",
                new Dictionary<string, object?>()),
            "site_crawl",
            artifact);

        var rawJson = entry.Outputs["_artifactJson"]?.ToString() ?? string.Empty;
        Assert.DoesNotContain(new string('x', 100), rawJson);

        var roundTripped = SiteAnalyzerStepArtifactStore.GetRequiredArtifact<SiteAnalyzerStepArtifactStore.SiteStructureArtifact>(
            [entry],
            "site_crawl",
            "site_crawl");
        Assert.Equal(string.Empty, roundTripped.Crawl.Pages[0].Html);
        Assert.Equal("https://example.com/", roundTripped.Crawl.Pages[0].Url);
    }

    [Fact]
    public void TryGetArtifact_ReturnsNullAfterStepLogPersistenceSlimming()
    {
        var entry = SiteAnalyzerStepArtifactStore.WithArtifact(
            new SiteAnalysisStepLogEntry(
                12,
                "profile",
                "site analysis profile",
                "complete",
                "saved",
                new Dictionary<string, object?>()),
            "profile",
            new { PrimaryFocus = "IT support", AudienceType = "local_service", FocusTags = new[] { "msp" } });

        var slim = SiteAnalyzerStepArtifactStore.ForStepLogPersistence(entry);

        Assert.Null(
            SiteAnalyzerStepArtifactStore.TryGetArtifact<object>([slim], "profile", "profile"));
    }

    [Fact]
    public void ForStepLogPersistence_RemovesArtifactPayload()
    {
        var entry = SiteAnalyzerStepArtifactStore.WithArtifact(
            new SiteAnalysisStepLogEntry(
                6,
                "site_crawl",
                "Site crawl",
                "complete",
                "saved",
                new Dictionary<string, object?> { ["pagesCrawled"] = 2 }),
            "site_crawl",
            new SampleArtifact("crawl", 2));

        var slim = SiteAnalyzerStepArtifactStore.ForStepLogPersistence(entry);

        Assert.False(slim.Outputs.ContainsKey("_artifactJson"));
        Assert.False(slim.Outputs.ContainsKey("_artifactType"));
        Assert.Equal(2, slim.Outputs["pagesCrawled"]);
    }
}
