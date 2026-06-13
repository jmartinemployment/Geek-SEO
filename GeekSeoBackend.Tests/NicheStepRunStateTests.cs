using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services.NicheStepRunners;

namespace GeekSeoBackend.Tests;

public sealed class NicheStepRunStateTests
{
    [Fact]
    public void ResolveMergedFusion_UsesMergingArtifactWhenSnapshotMissing()
    {
        var fusion = new SiteTopicProfile
        {
            AllCandidates =
            [
                new TopicCandidate
                {
                    Name = "Managed IT",
                    Slug = "managed-it",
                    Confidence = 0.8m,
                    Evidence = [new TopicEvidence { Source = "schema", Weight = 0.35m }],
                },
            ],
            SelectedPillars =
            [
                new TopicCandidate
                {
                    Name = "Managed IT",
                    Slug = "managed-it",
                    Confidence = 0.8m,
                    Evidence = [new TopicEvidence { Source = "schema", Weight = 0.35m }],
                },
            ],
            ExcludedCandidates = [],
            ExclusionReasons = new Dictionary<string, string>(),
            SulVersion = "sul-2.0",
            SignalSourcesPresent = ["schema"],
        };

        var mergingEntry = NicheStepArtifactStore.WithArtifact(
            new NicheAnalysisStepLogEntry(
                7,
                "merging",
                "Topic selection",
                "complete",
                "saved",
                new Dictionary<string, object?>()),
            "merging",
            fusion);

        var resolved = NicheStepRunState.ResolveMergedFusionSnapshot(null, [mergingEntry]);

        Assert.NotNull(resolved);
        Assert.Equal("sul-2.0", resolved!.SulVersion);
        Assert.Single(resolved.SelectedPillars);
        Assert.Equal("managed-it", resolved.SelectedPillars[0].Slug);
    }
}
