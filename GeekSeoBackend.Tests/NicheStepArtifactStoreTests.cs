using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.NicheExtraction;
using GeekSeoBackend.Services.NicheStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class NicheStepArtifactStoreTests
{
    private sealed record SampleArtifact(string Value, int Count);

    [Fact]
    public void WithArtifact_StoresRoundTrippablePayload()
    {
        var entry = new NicheAnalysisStepLogEntry(
            7,
            "merging",
            "Topic selection",
            "complete",
            "saved",
            new Dictionary<string, object?>());

        var stored = NicheStepArtifactStore.WithArtifact(
            entry,
            "sample",
            new SampleArtifact("topic-pool", 3));

        var artifact = NicheStepArtifactStore.GetRequiredArtifact<SampleArtifact>(
            [stored],
            "merging",
            "sample");

        Assert.Equal("topic-pool", artifact.Value);
        Assert.Equal(3, artifact.Count);
    }

    [Fact]
    public void TryGetArtifact_ReturnsNullWhenArtifactTypeDoesNotMatch()
    {
        var entry = NicheStepArtifactStore.WithArtifact(
            new NicheAnalysisStepLogEntry(
                8,
                "keywords",
                "Keyword demand",
                "complete",
                "saved",
                new Dictionary<string, object?>()),
            "keywords",
            new SampleArtifact("demand", 1));

        var artifact = NicheStepArtifactStore.TryGetArtifact<SampleArtifact>(
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
        var artifact = new NicheStepArtifactStore.SiteStructureArtifact(
            crawl,
            new InternalLinkData([], new Dictionary<string, int>(), 1),
            new UrlPatternData([], 0),
            ["https://example.com/"]);

        var entry = NicheStepArtifactStore.WithArtifact(
            new NicheAnalysisStepLogEntry(
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

        var roundTripped = NicheStepArtifactStore.GetRequiredArtifact<NicheStepArtifactStore.SiteStructureArtifact>(
            [entry],
            "site_crawl",
            "site_crawl");
        Assert.Equal(string.Empty, roundTripped.Crawl.Pages[0].Html);
        Assert.Equal("https://example.com/", roundTripped.Crawl.Pages[0].Url);
    }
}
