using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

public sealed class PillarMerger
{
    private const int MinPillars = 3;

    // Priority: schema > sitemap > nav > heading
    private static readonly Dictionary<string, int> SourcePriority = new(StringComparer.OrdinalIgnoreCase)
    {
        ["schema"] = 0, ["sitemap"] = 1, ["nav"] = 2, ["heading"] = 3,
    };

    public PillarMergeResult Merge(
        IReadOnlyList<DiscoveredPillar> schema,
        IReadOnlyList<DiscoveredPillar> sitemap,
        IReadOnlyList<DiscoveredPillar> nav,
        IReadOnlyList<DiscoveredPillar> headings,
        IReadOnlyList<string> locationFallbacks)
    {
        var merged = new Dictionary<string, DiscoveredPillar>(StringComparer.OrdinalIgnoreCase);

        // Add in priority order — higher priority wins on slug collision
        AddRange(merged, schema, "schema");
        AddRange(merged, sitemap, "sitemap");
        AddRange(merged, nav, "nav");

        // Headings only if total < MinPillars
        if (merged.Count < MinPillars)
            AddRange(merged, headings, "heading");

        // Validate gates
        var validator = new PillarValidator();
        var candidates = merged.Values.ToList();

        // Gate 3 first (cheap filter)
        candidates = candidates.Where(validator.PassesGate3).ToList();

        // Gate 2 — merge similar pillars
        var toRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pairs = validator.FindMergePairs(candidates);
        foreach (var (keep, merge) in pairs)
            toRemove.Add(merge.Slug);
        candidates = candidates.Where(c => !toRemove.Contains(c.Slug)).ToList();

        // If still < MinPillars, add location pillars
        if (candidates.Count < MinPillars)
        {
            foreach (var loc in locationFallbacks.Take(MinPillars - candidates.Count))
            {
                var slug = loc.ToLowerInvariant().Replace(' ', '-');
                if (!candidates.Any(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)))
                {
                    candidates.Add(new DiscoveredPillar
                    {
                        Name = loc,
                        Slug = slug,
                        Intent = "local",
                        Source = "schema",
                        ChildPageCount = 1,
                    });
                }
            }
        }

        // Sort: schema first, then by ChildPageCount desc
        var selected = candidates
            .OrderBy(c => SourcePriority.GetValueOrDefault(c.Source, 99))
            .ThenByDescending(c => c.ChildPageCount)
            .ToList();

        return new PillarMergeResult(selected, []);
    }

    private static void AddRange(
        Dictionary<string, DiscoveredPillar> merged,
        IReadOnlyList<DiscoveredPillar> source,
        string sourceTag)
    {
        foreach (var pillar in source)
        {
            var slug = pillar.Slug;
            if (merged.ContainsKey(slug))
            {
                // Enrich existing with nav child slugs if this is a nav source
                if (sourceTag == "nav" && pillar.ChildSlugs.Count > 0)
                {
                    var existing = merged[slug];
                    merged[slug] = existing with
                    {
                        ChildSlugs = existing.ChildSlugs.Union(pillar.ChildSlugs).ToList(),
                        PageUrl = existing.PageUrl ?? pillar.PageUrl,
                    };
                }
            }
            else
            {
                merged[slug] = pillar with { Source = sourceTag };
            }
        }
    }
}
