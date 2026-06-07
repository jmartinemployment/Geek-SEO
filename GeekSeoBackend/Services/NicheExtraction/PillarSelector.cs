using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Selects pillars from the topic candidate pool using documented search engine behavior:
/// schema/GSC-confirmed topics are unconditionally accepted; all others require a minimum
/// confidence score. Deduplication (Gate 2) and noise filtering (Gate 3) are applied first.
/// </summary>
public sealed class PillarSelector(PillarValidator validator)
{
    public const string SulVersion = "sul-2.0";
    private const int MinPillars = 3;
    internal const int MaxDisplayPillars = 15;
    private const int MinInboundLinksForInternalLinkOnly = 2;

    public SiteTopicProfile Select(
        IReadOnlyList<TopicCandidate> pool,
        IReadOnlyList<string> locationFallbacks)
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

        // Gate 2 — dedup: merge near-synonyms, keep higher-confidence slug
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

        // Location fallbacks: inject when too few pillars survive (sites with no location URL structure)
        if (afterGate2.Count < MinPillars)
        {
            foreach (var loc in locationFallbacks.Take(MinPillars - afterGate2.Count))
            {
                var slug = NicheAnalyzerService.NameToSlug(loc);
                if (afterGate2.Any(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)))
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
                afterGate2.Add(ToDiscoveredPillar(fallback));
            }
        }

        var candidateBySlug = workingPool.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);

        // Selection rules (mirrors documented SE behavior):
        //   Schema-declared → unconditional (site owner assertion; Google/Bing treat as authoritative)
        //   GSC-confirmed   → unconditional (SE already associates this topic with the site)
        //   All others      → confidence >= MinPillarConfidence (nav-level signal or stronger)
        var selectedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pillar in afterGate2)
        {
            if (!candidateBySlug.TryGetValue(pillar.Slug, out var candidate))
            {
                selectedSlugs.Add(pillar.Slug);
                continue;
            }

            var hasSchema = candidate.Evidence.Any(e => e.Source is "schema" or "same_as");
            var hasGsc = candidate.Evidence.Any(e => e.Source == "gsc");

            if (hasSchema || hasGsc)
            {
                selectedSlugs.Add(pillar.Slug);
                continue;
            }

            if (IsWeakInternalLinkOnly(candidate))
            {
                exclusionReasons.TryAdd(pillar.Slug,
                    "Internal link signal only — needs corroboration or 2+ inbound links");
                continue;
            }

            if (candidate.Confidence >= TopicEvidenceWeights.MinPillarConfidence)
                selectedSlugs.Add(pillar.Slug);
            else
                exclusionReasons.TryAdd(pillar.Slug,
                    $"Insufficient signal strength (confidence {candidate.Confidence:F2} < {TopicEvidenceWeights.MinPillarConfidence:F2})");
        }

        var selected = ApplySoftCap(
            workingPool.Where(c => selectedSlugs.Contains(c.Slug)).ToList(),
            exclusionReasons);
        var excluded = workingPool
            .Where(c => !selectedSlugs.Contains(c.Slug))
            .ToList();

        return new SiteTopicProfile
        {
            AllCandidates = workingPool,
            SelectedPillars = selected,
            ExcludedCandidates = excluded,
            ExclusionReasons = exclusionReasons,
            SulVersion = SulVersion,
            SignalSourcesPresent = signalSources,
        };
    }

    public PillarMergeResult ToPillarMergeResult(SiteTopicProfile profile)
    {
        var selected = profile.SelectedPillars.Select(ToDiscoveredPillar).ToList();
        var excluded = profile.ExcludedCandidates.Select(ToDiscoveredPillar).ToList();
        return new PillarMergeResult(selected, excluded);
    }

    private static bool IsWeakInternalLinkOnly(TopicCandidate candidate)
    {
        var sources = candidate.Evidence
            .Select(e => e.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var internalLinkOnly = sources.Count == 1
            && sources[0].Equals("internal_link", StringComparison.OrdinalIgnoreCase);

        return internalLinkOnly && candidate.InternalLinkCount < MinInboundLinksForInternalLinkOnly;
    }

    private static List<TopicCandidate> ApplySoftCap(
        IReadOnlyList<TopicCandidate> selected,
        IDictionary<string, string> exclusionReasons)
    {
        if (selected.Count <= MaxDisplayPillars)
            return selected
                .OrderByDescending(c => c.Confidence)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

        static bool IsProtected(TopicCandidate c) =>
            c.Evidence.Any(e => e.Source is "schema" or "same_as" or "gsc");

        var protectedPillars = selected.Where(IsProtected).ToList();
        var optional = selected
            .Where(c => !IsProtected(c))
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var slots = Math.Max(0, MaxDisplayPillars - protectedPillars.Count);
        var keptOptional = optional.Take(slots).ToList();
        foreach (var dropped in optional.Skip(slots))
        {
            exclusionReasons.TryAdd(
                dropped.Slug,
                $"Exceeded soft pillar cap ({MaxDisplayPillars}); lower confidence than selected topics");
        }

        return protectedPillars
            .Concat(keptOptional)
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
            ChildPageCount = candidate.InternalLinkCount,
            ContentDepthScore = candidate.ContentDepthScore,
            ChildSlugs = [],
        };
    }
}
