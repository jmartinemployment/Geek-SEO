using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

public sealed class PillarValidator
{
    private const double MergeSimilarityThreshold = 0.6;
    private const int MinSubtopicCapacity = 3;

    // Gate 1 — Semantic Scope: pillar must support ≥ MinSubtopicCapacity sub-topics
    public bool PassesGate1(DiscoveredPillar pillar) =>
        pillar.ChildPageCount >= MinSubtopicCapacity ||
        pillar.ChildSlugs.Count >= 2 ||
        IsGenericServiceTerm(pillar.Slug);

    // Gate 2 — Search Intent Alignment: returns pairs to merge (similarity > threshold)
    public IReadOnlyList<(DiscoveredPillar Keep, DiscoveredPillar Merge)> FindMergePairs(
        IReadOnlyList<DiscoveredPillar> pillars)
    {
        var pairs = new List<(DiscoveredPillar, DiscoveredPillar)>();
        var sourcePriority = new Dictionary<string, int>
        {
            ["schema"] = 0, ["sitemap"] = 1, ["nav"] = 2, ["heading"] = 3,
        };

        for (var i = 0; i < pillars.Count; i++)
        {
            for (var j = i + 1; j < pillars.Count; j++)
            {
                var sim = PillarSynonymMap.Similarity(pillars[i].Slug, pillars[j].Slug);
                if (sim <= MergeSimilarityThreshold) continue;

                var priorityI = sourcePriority.GetValueOrDefault(pillars[i].Source, 99);
                var priorityJ = sourcePriority.GetValueOrDefault(pillars[j].Source, 99);

                var keep = priorityI <= priorityJ ? pillars[i] : pillars[j];
                var merge = priorityI <= priorityJ ? pillars[j] : pillars[i];
                pairs.Add((keep, merge));
            }
        }

        return pairs;
    }

    // Gate 3 — Commercial/Topical Relevance
    public bool PassesGate3(DiscoveredPillar pillar) =>
        !NoisePaths.IsNoise(pillar.Slug) && !IsH2Noise(pillar.Name);

    private static bool IsGenericServiceTerm(string slug)
    {
        var serviceTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "repair", "installation", "maintenance", "consulting", "services",
            "solutions", "support", "management", "setup", "training",
            "recovery", "removal", "security", "networking", "monitoring",
        };
        var tokens = slug.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return tokens.Any(t => serviceTerms.Contains(t));
    }

    private static bool IsH2Noise(string name) =>
        NoisePaths.H2Noise.Contains(name.Trim());
}
