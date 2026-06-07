using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>Applies entity coverage (Gap 3) and internal link graph (Gap 5) to the topic profile.</summary>
internal static class TopicSnapshotEnricher
{
    internal static SiteTopicProfile Apply(
        SiteTopicProfile profile,
        InternalLinkData internalLinks,
        UrlPatternData urlPatterns,
        IReadOnlyList<PillarSerpEnrichment> serpValidations)
    {
        var entityCoverage = EntityCoverageScorer.Compute(profile, serpValidations);
        var linkGraph = InternalLinkGraphBuilder.Build(profile, internalLinks, urlPatterns);

        var enriched = profile with
        {
            EntityCoverageBySlug = entityCoverage,
            InternalLinkGraph = linkGraph,
        };

        return enriched with
        {
            RecommendedActions = PillarActionRecommender.Recommend(enriched),
        };
    }
}
