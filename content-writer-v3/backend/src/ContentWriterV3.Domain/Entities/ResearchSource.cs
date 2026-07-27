namespace ContentWriterV3.Domain.Entities;

public class ResearchSource : BaseEntity
{
    public Guid ResearchRunId { get; set; }
    public ResearchSourceType SourceType { get; set; }
    public string? Url { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ResearchEvidence> Evidence { get; set; } = new();

    public ResearchSource() { }

    public ResearchSource(Guid researchRunId, ResearchSourceType sourceType, string title)
    {
        ResearchRunId = researchRunId;
        SourceType = sourceType;
        Title = title;
    }
}

public enum ResearchSourceType
{
    ExistingInternal,
    OperatorUploaded,
    AgentDiscoveredExternal
}

public class ResearchEvidence : BaseEntity
{
    public Guid ResearchSourceId { get; set; }
    public string Statement { get; set; } = string.Empty;
    public EvidenceSupportLevel SupportLevel { get; set; }
    public bool ApprovedForClaim { get; set; }
    public int Confidence { get; set; } = 50; // 0-100

    public ResearchEvidence() { }

    public ResearchEvidence(Guid sourceId, string statement, EvidenceSupportLevel supportLevel, int confidence)
    {
        ResearchSourceId = sourceId;
        Statement = statement;
        SupportLevel = supportLevel;
        Confidence = confidence;
    }
}

public enum EvidenceSupportLevel
{
    VerifiedClientFact,      // Tier 1: Highest confidence, from client-provided data
    VerifiedExternalSource,  // Tier 2: Verified third-party source
    ObservedMarketLanguage,  // Tier 3: Observed in market, not explicitly verified
    Unsupported              // No supporting evidence found
}
