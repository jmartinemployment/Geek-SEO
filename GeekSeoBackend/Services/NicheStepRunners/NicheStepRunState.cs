using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheStepRunners;

internal static class NicheStepRunState
{
    public static async Task<SiteTopicProfile?> LoadMergedFusionSnapshotAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        string? persistedFusionSnapshot,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var persisted = ResolveMergedFusionSnapshot(persistedFusionSnapshot, steps);
        var candidates = await LoadAllTopicCandidatesAsync(profileRepo, profileId, ct);
        if (candidates.Count == 0)
            return persisted;

        var allCandidates = candidates
            .OrderBy(c => c.DisplayOrder)
            .Select(ToTopicCandidate)
            .ToList();
        var selectedPillars = candidates
            .Where(c => c.IsSelected)
            .OrderBy(c => c.DisplayOrder)
            .Select(ToTopicCandidate)
            .ToList();
        var excludedCandidates = candidates
            .Where(c => !c.IsSelected)
            .OrderBy(c => c.DisplayOrder)
            .Select(ToTopicCandidate)
            .ToList();
        var exclusionReasons = candidates
            .Where(c => !c.IsSelected && !string.IsNullOrWhiteSpace(c.ExclusionReason))
            .ToDictionary(c => c.Slug, c => c.ExclusionReason!, StringComparer.OrdinalIgnoreCase);
        var signalSources = allCandidates
            .SelectMany(c => c.Evidence.Select(e => e.Source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (persisted is not null)
        {
            return persisted with
            {
                AllCandidates = allCandidates,
                SelectedPillars = selectedPillars,
                ExcludedCandidates = excludedCandidates,
                ExclusionReasons = exclusionReasons,
                SignalSourcesPresent = signalSources.Count > 0 ? signalSources : persisted.SignalSourcesPresent,
            };
        }

        return new SiteTopicProfile
        {
            AllCandidates = allCandidates,
            SelectedPillars = selectedPillars,
            ExcludedCandidates = excludedCandidates,
            ExclusionReasons = exclusionReasons,
            SulVersion = "relational-candidates",
            SignalSourcesPresent = signalSources,
            NormalizedTopicalityBySlug = new Dictionary<string, decimal>(),
            EntityCoverageBySlug = new Dictionary<string, PillarEntityCoverage>(),
            RecommendedActions = [],
        };
    }

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

    private static async Task<List<NicheTopicCandidatePage>> LoadAllTopicCandidatesAsync(
        INicheProfileRepository profileRepo,
        Guid profileId,
        CancellationToken ct)
    {
        const int pageSize = 500;
        var page = 1;
        var items = new List<NicheTopicCandidatePage>();

        while (true)
        {
            var result = await profileRepo.GetTopicCandidatesAsync(
                profileId,
                page,
                pageSize,
                selectedOnly: null,
                ct);
            if (!result.IsSuccess)
                throw new InvalidOperationException(
                    result.Error ?? "Failed to load relational topic candidates.");
            if (result.Value is null)
                throw new InvalidOperationException("Topic candidate response was empty.");

            items.AddRange(result.Value.Items);
            if (items.Count >= result.Value.Total || result.Value.Items.Count == 0)
                break;

            page++;
        }

        return items;
    }

    private static TopicCandidate ToTopicCandidate(NicheTopicCandidatePage page) =>
        new()
        {
            Name = page.Name,
            Slug = page.Slug,
            Evidence = page.Evidence ?? [],
            Confidence = page.Confidence,
            ContentDepthScore = page.ContentDepthScore,
            DedicatedPageUrl = page.DedicatedPageUrl,
            InternalLinkCount = page.InternalLinkCount,
        };
}
