using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Fuses peer-level topic candidates by confidence, then applies pillar validator gates.
/// </summary>
public sealed class TopicFusionEngine(PillarValidator validator)
{
    public const string FusionVersion = "sul-1.3";
    private const int MinPillars = 3;

    public FusedSiteUnderstanding Fuse(
        IReadOnlyList<TopicCandidate> pool,
        IReadOnlyList<string> locationFallbacks,
        int maxPillars = PillarMerger.DefaultPillarCap)
    {
        var exclusionReasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var workingPool = pool.ToList();
        var confidenceBySlug = workingPool.ToDictionary(
            c => c.Slug,
            c => c.Confidence,
            StringComparer.OrdinalIgnoreCase);

        var signalSources = workingPool
            .SelectMany(c => c.Evidence.Select(e => e.Source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pillars = workingPool.Select(ToDiscoveredPillar).ToList();

        // Gate 3 — noise filter
        var afterGate3 = pillars.Where(validator.PassesGate3).ToList();
        foreach (var dropped in pillars.Except(afterGate3))
            exclusionReasons[dropped.Slug] = "Failed relevance gate (noise or generic heading)";

        // Gate 2 — merge similar topics (keep higher-confidence slug)
        var toRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (keep, merge) in validator.FindMergePairs(afterGate3))
        {
            var keepSlug = confidenceBySlug.GetValueOrDefault(keep.Slug) >= confidenceBySlug.GetValueOrDefault(merge.Slug)
                ? keep.Slug
                : merge.Slug;
            var mergeSlug = keepSlug.Equals(keep.Slug, StringComparison.OrdinalIgnoreCase) ? merge.Slug : keep.Slug;
            if (!toRemove.Add(mergeSlug))
                continue;

            var keepName = workingPool.FirstOrDefault(c => c.Slug.Equals(keepSlug, StringComparison.OrdinalIgnoreCase))?.Name
                           ?? keepSlug;
            exclusionReasons[mergeSlug] = $"Merged with similar topic \"{keepName}\"";
        }

        var afterGate2 = afterGate3.Where(p => !toRemove.Contains(p.Slug)).ToList();

        // Gate 1 — scope check
        var afterGate1 = afterGate2.Where(validator.PassesGate1).ToList();
        foreach (var dropped in afterGate2.Except(afterGate1))
            exclusionReasons.TryAdd(dropped.Slug, "Insufficient subtopic capacity (Gate 1)");

        // Location fallbacks if still thin
        if (afterGate1.Count < MinPillars)
        {
            foreach (var loc in locationFallbacks.Take(MinPillars - afterGate1.Count))
            {
                var slug = NicheAnalyzerService.NameToSlug(loc);
                if (afterGate1.Any(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var fallback = new TopicCandidate
                {
                    Name = loc,
                    Slug = slug,
                    Confidence = TopicEvidenceWeights.Schema,
                    Evidence =
                    [
                        new TopicEvidence
                        {
                            Source = "schema",
                            Snippet = "areaServed fallback",
                            Weight = TopicEvidenceWeights.Schema,
                        },
                    ],
                };
                workingPool.Add(fallback);
                confidenceBySlug[slug] = fallback.Confidence;
                afterGate1.Add(ToDiscoveredPillar(fallback));
            }
        }

        var cap = ComputePillarCap(maxPillars, afterGate1, workingPool);
        var candidateBySlug = workingPool.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);
        var schemaSlugs = workingPool
            .Where(c => c.Evidence.Any(e => e.Source == "schema"))
            .Select(c => c.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var guaranteedSchema = afterGate1
            .Where(p => schemaSlugs.Contains(p.Slug))
            .Select(p => p.Slug)
            .ToList();

        var rankedSlugs = afterGate1
            .Select(p => p.Slug)
            .Where(slug => !schemaSlugs.Contains(slug))
            .Where(slug =>
            {
                if (!candidateBySlug.TryGetValue(slug, out var candidate))
                    return false;

                if (TopicCorroboration.PassesCorroboration(candidate.Evidence))
                    return true;

                exclusionReasons.TryAdd(slug, "Insufficient signal corroboration (single weak source)");
                return false;
            })
            .OrderByDescending(slug => confidenceBySlug.GetValueOrDefault(slug))
            .ThenBy(slug => slug, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var selectedSlugs = guaranteedSchema.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var slug in rankedSlugs)
        {
            if (selectedSlugs.Count >= cap)
                break;
            selectedSlugs.Add(slug);
        }

        foreach (var slug in afterGate1.Select(p => p.Slug).Where(slug => !selectedSlugs.Contains(slug)))
            exclusionReasons.TryAdd(slug, $"Below pillar cap ({cap})");

        var selected = workingPool
            .Where(c => selectedSlugs.Contains(c.Slug))
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var excluded = workingPool
            .Where(c => !selectedSlugs.Contains(c.Slug))
            .ToList();

        return new FusedSiteUnderstanding
        {
            AllCandidates = workingPool,
            SelectedPillars = selected,
            ExcludedCandidates = excluded,
            ExclusionReasons = exclusionReasons,
            FusionVersion = FusionVersion,
            SignalSourcesPresent = signalSources,
            PillarCap = cap,
        };
    }

    public PillarMergeResult ToPillarMergeResult(FusedSiteUnderstanding fused)
    {
        var selected = fused.SelectedPillars.Select(ToDiscoveredPillar).ToList();
        var excludedByCap = fused.ExcludedCandidates
            .Where(c =>
                fused.ExclusionReasons.TryGetValue(c.Slug, out var reason)
                && reason.StartsWith("Below pillar cap", StringComparison.Ordinal))
            .Select(ToDiscoveredPillar)
            .ToList();

        return new PillarMergeResult(selected, excludedByCap, fused.PillarCap);
    }

    internal static DiscoveredPillar ToDiscoveredPillar(TopicCandidate candidate)
    {
        var primarySource = candidate.Evidence
            .OrderByDescending(e => e.Weight)
            .ThenBy(e => e.Source, StringComparer.OrdinalIgnoreCase)
            .First()
            .Source;

        return new DiscoveredPillar
        {
            Name = candidate.Name,
            Slug = candidate.Slug,
            PageUrl = candidate.DedicatedPageUrl,
            Intent = primarySource is "heading" or "page" or "page_vertical"
                ? "informational"
                : "commercial",
            Source = primarySource,
            ChildPageCount = Math.Max(candidate.InternalLinkCount, 3),
            ChildSlugs = [],
        };
    }

    private static int ComputePillarCap(
        int maxPillars,
        IReadOnlyList<DiscoveredPillar> afterGate1,
        IReadOnlyList<TopicCandidate> pool)
    {
        var schemaSlugs = pool
            .Where(c => c.Evidence.Any(e => e.Source == "schema"))
            .Select(c => c.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var schemaSelected = afterGate1.Count(p => schemaSlugs.Contains(p.Slug));

        // Reserve room for all schema topics; page verticals (e.g. Accounting) compete within maxPillars.
        var floor = schemaSelected;
        return Math.Max(MinPillars, Math.Max(maxPillars, floor));
    }
}
