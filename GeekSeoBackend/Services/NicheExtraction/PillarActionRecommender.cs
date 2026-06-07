using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>Derives Phase E draft actions from the topic profile snapshot (no side effects).</summary>
internal static class PillarActionRecommender
{
    internal const decimal MinConfidenceForPageSuggestion = 0.45m;
    internal const decimal MinConfidenceForSchemaSync = 0.40m;

    internal static IReadOnlyList<PillarRecommendedAction> Recommend(SiteTopicProfile profile)
    {
        var actions = new List<PillarRecommendedAction>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pillar in profile.SelectedPillars)
        {
            if (string.IsNullOrWhiteSpace(pillar.DedicatedPageUrl)
                && pillar.Confidence >= MinConfidenceForPageSuggestion)
            {
                TryAdd(actions, seen, new PillarRecommendedAction(
                    "suggest_pillar_page",
                    pillar.Slug,
                    pillar.Name,
                    "High-confidence topic with no dedicated URL — draft a pillar page.",
                    Math.Round(0.75m + pillar.Confidence * 0.1m, 4)));
            }

            var hasSchema = pillar.Evidence.Any(e =>
                string.Equals(e.Source, "schema", StringComparison.OrdinalIgnoreCase));
            var hasPublicStructure = pillar.Evidence.Any(e =>
                e.Source is "page" or "page_vertical" or "nav" or "sitemap" or "url_pattern");

            if (!hasSchema && hasPublicStructure && pillar.Confidence >= MinConfidenceForSchemaSync)
            {
                TryAdd(actions, seen, new PillarRecommendedAction(
                    "schema_sync",
                    pillar.Slug,
                    pillar.Name,
                    "Page or nav signals this topic but schema.org does not — add to knowsAbout or offerCatalog.",
                    Math.Round(0.65m + pillar.Confidence * 0.1m, 4)));
            }
        }

        foreach (var coverage in profile.EntityCoverageBySlug.Values.Where(c => c.IsEntityThin))
        {
            TryAdd(actions, seen, new PillarRecommendedAction(
                "entity_thin_content",
                coverage.Slug,
                coverage.Name,
                $"SERP expects {coverage.ExpectedEntityCount} related entities; site covers {coverage.MatchedEntityCount} — expand content cluster.",
                0.9m));
        }

        var orphans = profile.InternalLinkGraph?.OrphanSlugs ?? [];
        foreach (var orphanSlug in orphans)
        {
            var pillar = profile.SelectedPillars.FirstOrDefault(p =>
                p.Slug.Equals(orphanSlug, StringComparison.OrdinalIgnoreCase));
            var name = pillar?.Name ?? SitemapExtractor.SlugToTitle(orphanSlug);

            TryAdd(actions, seen, new PillarRecommendedAction(
                "link_orphan_pillar",
                orphanSlug,
                name,
                "Selected pillar has no internal links to or from other pillars — add hub links.",
                0.55m));
        }

        foreach (var gap in profile.LocalGeography?.Gaps ?? [])
        {
            TryAdd(actions, seen, new PillarRecommendedAction(
                "suggest_local_page",
                gap.SuggestedSlug,
                gap.SuggestedTitle,
                gap.Reason,
                0.72m));
        }

        return actions
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.TopicName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void TryAdd(
        ICollection<PillarRecommendedAction> actions,
        ISet<string> seen,
        PillarRecommendedAction action)
    {
        var key = $"{action.ActionType}:{action.TopicSlug}";
        if (!seen.Add(key))
            return;

        actions.Add(action);
    }
}
