using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Compares site topic entities to SERP-derived expected entities per pillar (Gap 3).
/// </summary>
internal static class EntityCoverageScorer
{
    internal const decimal EntityThinThreshold = 0.60m;

    internal static IReadOnlyDictionary<string, PillarEntityCoverage> Compute(
        FusedSiteUnderstanding fused,
        IReadOnlyList<PillarSerpEnrichment> serpValidations)
    {
        if (fused.SelectedPillars.Count == 0 || serpValidations.Count == 0)
            return new Dictionary<string, PillarEntityCoverage>();

        var siteSlugs = fused.AllCandidates
            .Select(c => c.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var siteNamesBySlug = fused.AllCandidates.ToDictionary(
            c => c.Slug,
            c => c.Name,
            StringComparer.OrdinalIgnoreCase);

        var bySlug = new Dictionary<string, PillarEntityCoverage>(StringComparer.OrdinalIgnoreCase);

        foreach (var pillar in fused.SelectedPillars)
        {
            var serp = serpValidations.FirstOrDefault(v =>
                v.Slug.Equals(pillar.Slug, StringComparison.OrdinalIgnoreCase));
            var expectedTopics = serp?.ExpectedTopicSlugs ?? [];
            if (serp is null || expectedTopics.Count == 0)
            {
                bySlug[pillar.Slug] = new PillarEntityCoverage(
                    pillar.Slug,
                    pillar.Name,
                    1m,
                    0,
                    0,
                    [],
                    false);
                continue;
            }

            var expected = expectedTopics
                .Where(s => !s.Equals(pillar.Slug, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (expected.Count == 0)
            {
                bySlug[pillar.Slug] = new PillarEntityCoverage(
                    pillar.Slug,
                    pillar.Name,
                    1m,
                    0,
                    0,
                    [],
                    false);
                continue;
            }

            var matched = new List<string>();
            var missing = new List<string>();

            foreach (var topicSlug in expected)
            {
                if (SiteHasEntity(siteSlugs, siteNamesBySlug, topicSlug))
                    matched.Add(topicSlug);
                else
                    missing.Add(SitemapExtractor.SlugToTitle(topicSlug));
            }

            var score = Math.Round((decimal)matched.Count / expected.Count, 4);
            bySlug[pillar.Slug] = new PillarEntityCoverage(
                pillar.Slug,
                pillar.Name,
                score,
                expected.Count,
                matched.Count,
                missing.Take(8).ToList(),
                score < EntityThinThreshold);
        }

        return bySlug;
    }

    private static bool SiteHasEntity(
        IReadOnlySet<string> siteSlugs,
        IReadOnlyDictionary<string, string> siteNamesBySlug,
        string expectedSlug)
    {
        if (siteSlugs.Contains(expectedSlug))
            return true;

        var expectedPhrase = expectedSlug.Replace('-', ' ');
        foreach (var name in siteNamesBySlug.Values)
        {
            if (name.Contains(expectedPhrase, StringComparison.OrdinalIgnoreCase)
                || name.Contains(expectedSlug, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return siteSlugs.Any(s =>
            s.Contains(expectedSlug, StringComparison.OrdinalIgnoreCase)
            || expectedSlug.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}
