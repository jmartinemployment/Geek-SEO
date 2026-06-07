namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Shrinks <see cref="SiteTopicProfile"/> before DB persistence — full in-memory snapshot
/// can exceed gateway timeouts when 60+ pillars carry duplicate evidence payloads.
/// </summary>
public static class SiteTopicProfilePersistence
{
    private const int MaxEvidencePerCandidate = 4;
    private const int MaxSnippetLength = 120;
    private const int MaxLinkGraphEdges = 250;
    private const int MaxSampleAnchors = 2;
    private const int MaxMissingEntities = 8;

    public static SiteTopicProfile Trim(SiteTopicProfile profile) =>
        profile with
        {
            AllCandidates = TrimCandidates(profile.AllCandidates),
            SelectedPillars = TrimCandidates(profile.SelectedPillars, stripEvidence: true),
            ExcludedCandidates = [],
            EntityCoverageBySlug = TrimEntityCoverage(profile.EntityCoverageBySlug),
            InternalLinkGraph = TrimLinkGraph(profile.InternalLinkGraph),
        };

    private static IReadOnlyList<TopicCandidate> TrimCandidates(
        IReadOnlyList<TopicCandidate> candidates,
        bool stripEvidence = false) =>
        candidates.Select(c => TrimCandidate(c, stripEvidence)).ToList();

    private static TopicCandidate TrimCandidate(TopicCandidate candidate, bool stripEvidence)
    {
        if (stripEvidence)
        {
            return candidate with { Evidence = [] };
        }

        var evidence = candidate.Evidence
            .OrderByDescending(e => e.Weight)
            .Take(MaxEvidencePerCandidate)
            .Select(e => e with
            {
                Snippet = Truncate(e.Snippet, MaxSnippetLength),
            })
            .ToList();

        return candidate with { Evidence = evidence };
    }

    private static IReadOnlyDictionary<string, PillarEntityCoverage> TrimEntityCoverage(
        IReadOnlyDictionary<string, PillarEntityCoverage>? coverage)
    {
        if (coverage is null || coverage.Count == 0)
            return coverage ?? new Dictionary<string, PillarEntityCoverage>();

        return coverage.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var value = kvp.Value;
                return value with
                {
                    MissingEntities = value.MissingEntities.Take(MaxMissingEntities).ToList(),
                };
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private static InternalLinkGraph? TrimLinkGraph(InternalLinkGraph? graph)
    {
        if (graph is null)
            return null;

        var edges = graph.Edges
            .OrderByDescending(e => e.LinkCount)
            .Take(MaxLinkGraphEdges)
            .Select(e => e with
            {
                SampleAnchors = e.SampleAnchors.Take(MaxSampleAnchors).ToList(),
            })
            .ToList();

        return graph with { Edges = edges };
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength] + "…";
    }
}
