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
    /// Schema-declared topics auto-select only when confidence meets this floor,
    /// ensuring at least one corroborating signal alongside the schema declaration.
    /// Schema alone = 0.35 — any additional signal pushes confidence above this threshold.
    /// </summary>
    internal const decimal SchemaConfidenceFloor = 0.40m;

    /// <summary>Minimum ContentDepthScore for Gate 1 (non-schema, non-structural topics).</summary>
    internal const decimal ContentDepthGateMin = 0.15m;
}
