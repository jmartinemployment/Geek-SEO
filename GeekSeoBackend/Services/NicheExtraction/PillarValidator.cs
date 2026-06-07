using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

public sealed class PillarValidator
{
    private const double MergeSimilarityThreshold = 0.6;

    // Gate 2 — Semantic Deduplication: merge near-synonyms, keep higher-authority slug.
    // Schema-vs-schema pairs are protected: two distinct declared services stay separate.
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
                if (string.Equals(pillars[i].Source, "schema", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(pillars[j].Source, "schema", StringComparison.OrdinalIgnoreCase))
                    continue;

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

    // Gate 3 — Noise filter: reject generic stop-words and H2 navigation noise.
    public bool PassesGate3(DiscoveredPillar pillar) =>
        !NoisePaths.IsNoise(pillar.Slug) && !IsH2Noise(pillar.Name);

    private static bool IsH2Noise(string name) =>
        NoisePaths.H2Noise.Contains(name.Trim());
}
