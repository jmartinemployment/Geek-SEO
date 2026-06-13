using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheStepRunners;

internal static class NicheStepRunState
{
    public static SiteTopicProfile? ResolveMergedFusionSnapshot(
        string? persistedFusionSnapshot,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps)
    {
        var fromArtifacts = NicheStepArtifactStore.TryGetArtifact<SiteTopicProfile>(
            steps,
            "merging",
            "merging");
        if (fromArtifacts is not null)
            return fromArtifacts;

        if (string.IsNullOrWhiteSpace(persistedFusionSnapshot))
            return null;

        return SiteTopicProfileJson.Parse(persistedFusionSnapshot);
    }
}
