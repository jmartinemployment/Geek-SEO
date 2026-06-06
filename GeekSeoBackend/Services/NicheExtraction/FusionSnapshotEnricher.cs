using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>Applies Gap 3 + Gap 5 enrichments to the fusion snapshot before persist.</summary>
internal static class FusionSnapshotEnricher
{
    internal static FusedSiteUnderstanding Apply(
        FusedSiteUnderstanding fused,
        InternalLinkData internalLinks,
        UrlPatternData urlPatterns,
        IReadOnlyList<PillarSerpEnrichment> serpValidations) =>
        fused with
        {
            EntityCoverageBySlug = EntityCoverageScorer.Compute(fused, serpValidations),
            InternalLinkGraph = InternalLinkGraphBuilder.Build(fused, internalLinks, urlPatterns),
        };
}
