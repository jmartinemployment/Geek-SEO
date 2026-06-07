namespace GeekSeoBackend.Services.NicheExtraction;

internal static class TopicEvidenceWeights
{
    internal const decimal Schema = 0.35m;
    internal const decimal SameAs = 0.30m;
    internal const decimal Sitemap = 0.25m;
    internal const decimal InternalLink = 0.20m;
    internal const decimal Nav = 0.15m;
    internal const decimal Page = 0.15m;
    internal const decimal PageVertical = 0.28m;
    internal const decimal UrlPattern = 0.12m;
    internal const decimal Heading = 0.10m;
    internal const decimal Gsc = 0.20m;
    internal const decimal MaxConfidence = 1.0m;

    /// <summary>
    /// Minimum confidence for topics to be selected as pillars (non-schema, non-GSC).
    /// Nav-level signal (0.15) passes; heading-only (0.10) does not.
    /// Mirrors SE behavior: any topic with a substantive structural or content signal is accepted.
    /// </summary>
    internal const decimal MinPillarConfidence = 0.15m;
}
