namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Gated business-voice requirements for research-backed pillar drafts.
/// Built from frozen site profile + keyword context — not generic SERP voice.
/// </summary>
public sealed record BusinessVoicePack
{
    public bool Enabled { get; init; }
    public required string Keyword { get; init; }
    public string SiteName { get; init; } = string.Empty;
    public string SiteUrl { get; init; } = string.Empty;
    public string GeoLabel { get; init; } = string.Empty;
    public bool IsImplementationConsultancy { get; init; }
    public IReadOnlyList<string> DeclaredCapabilities { get; init; } = [];
    public IReadOnlyList<string> SuggestedToolExamples { get; init; } = [];
    public IReadOnlyList<string> WritingRecommendations { get; init; } = [];
    public int MinimumConcreteExamples { get; init; } = 3;
    public bool RequiresTraditionalVsAiContrast { get; init; }
    public bool RequiresPerSectionContrast { get; init; }
    public bool RequiresCapabilityBridge { get; init; }
    public bool RequiresLocalMarketExamples { get; init; }
    public int MinimumLocalMarketExamples { get; init; } = 2;
    public required string CtaParagraphHtml { get; init; }
    public string DataQualityPhaseLabel { get; init; } = "Data Quality Assessment";
}
