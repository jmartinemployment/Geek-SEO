using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Selects pillars from the topic candidate pool using documented search engine behavior:
/// schema/GSC-confirmed topics are unconditionally accepted; all others require a minimum
/// confidence score. Deduplication (Gate 2) and noise filtering (Gate 3) are applied first.
/// </summary>
public sealed class PillarSelector(PillarValidator validator)
{
    public const string SulVersion = "sul-2.0";

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

        // No forced minimum pillar count / invented placeholder pillars: a site with genuinely
        // few real topics keeps few pillars. `locationFallbacks` (schema areaServed) already
        // flows in as schema-sourced candidates elsewhere in the pool; it is not re-injected
        // here just to pad the pillar count.
        var candidateBySlug = workingPool.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);

        // Selection rules (mirrors documented SE behavior):
        //   Schema-declared          → unconditional (site owner assertion; Google/Bing treat as authoritative)
        //   GSC-confirmed            → unconditional (SE already associates this topic with the site)
        //   Content-backed heading   → unconditional (real paragraph text under the heading is a
        //                              stricter, more honest signal than a synthetic confidence
        //                              score — see PageSection.HasOwnContent / TopicCandidatePoolBuilder)
        //   All others               → confidence >= MinPillarConfidence (nav-level signal or stronger)
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
            var hasContentBackedHeading = candidate.Evidence.Any(e => e.Source == "heading_content_backed");

            if (hasSchema || hasGsc || hasContentBackedHeading)
                selectedSlugs.Add(pillar.Slug);
            else if (candidate.Confidence >= TopicEvidenceWeights.MinPillarConfidence)
                selectedSlugs.Add(pillar.Slug);
            else
                exclusionReasons.TryAdd(pillar.Slug,
                    $"Insufficient signal strength (confidence {candidate.Confidence:F2} < {TopicEvidenceWeights.MinPillarConfidence:F2})");
        }

        var selected = workingPool
            .Where(c => selectedSlugs.Contains(c.Slug))
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    internal static DiscoveredPillar ToDiscoveredPillar(TopicCandidate candidate)
    {
        // A candidate should always carry at least one piece of evidence — that's what made it
        // a candidate. Guard rather than crash if one somehow doesn't: "unknown" is an honest
        // label for "no evidence recorded", not an invented reason.
        var primarySource = candidate.Evidence
            .OrderByDescending(e => e.Weight)
            .ThenBy(e => e.Source, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            ?.Source ?? "unknown";

        return new DiscoveredPillar
        {
            Name = candidate.Name,
            Slug = candidate.Slug,
            PageUrl = candidate.DedicatedPageUrl,
            Intent = primarySource is "heading" or "heading_content_backed" or "page" or "page_vertical"
                ? "informational"
                : "commercial",
            Source = primarySource,
            ChildPageCount = candidate.InternalLinkCount,
            ContentDepthScore = candidate.ContentDepthScore,
            ChildSlugs = [],
        };
    }
}
