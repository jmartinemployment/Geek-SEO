using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>Derives topical-map seed keywords from a fusion snapshot (same pillar list as site analyzer).</summary>
internal static class SiteTopicalMapSeedResolver
{
    // Used downstream to bound how many seeds get full topical-map expansion (unrelated to
    // pillar-selection minimums/caps — this is a separate, deliberate expansion-cost control).
    internal const int DefaultExpansionSeeds = 3;

    // No forced cap on topical-map seeds: every real recommended-action / selected-pillar
    // signal becomes a seed candidate. Do not invent placeholder seeds to hit a minimum either.
    internal static IReadOnlyList<string> ResolveSeeds(SiteTopicProfile fusion)
    {
        if (fusion.SelectedPillars.Count == 0)
            return [];

        var seeds = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            var trimmed = name.Trim();
            if (!seen.Add(trimmed))
                return;
            seeds.Add(trimmed);
        }

        foreach (var action in fusion.RecommendedActions
                     .OrderByDescending(a => a.Priority))
        {
            if (action.ActionType is "suggest_pillar_page" or "entity_thin_content")
                Add(action.TopicName);
        }

        foreach (var pillar in fusion.SelectedPillars.OrderByDescending(p => p.Confidence))
        {
            if (string.IsNullOrWhiteSpace(pillar.DedicatedPageUrl))
                Add(pillar.Name);
        }

        if (seeds.Count == 0)
        {
            foreach (var pillar in fusion.SelectedPillars)
                Add(pillar.Name);
        }

        return seeds;
    }

    internal static string? MatchPillarName(string keyword, IReadOnlyList<TopicCandidate> pillars)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return null;

        foreach (var pillar in pillars)
        {
            if (keyword.Contains(pillar.Name, StringComparison.OrdinalIgnoreCase)
                || pillar.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return pillar.Name;
            }

            var slugPhrase = pillar.Slug.Replace('-', ' ');
            if (keyword.Contains(slugPhrase, StringComparison.OrdinalIgnoreCase))
                return pillar.Name;
        }

        return null;
    }
}
