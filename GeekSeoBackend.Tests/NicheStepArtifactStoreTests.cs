using GeekSeo.Application.Models.Seo;
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
}
