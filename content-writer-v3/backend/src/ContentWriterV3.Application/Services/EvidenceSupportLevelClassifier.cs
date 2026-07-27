using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Application.Services;

public interface IEvidenceSupportLevelClassifier
{
    EvidenceSupportLevel ClassifyEvidence(ResearchSource source, string statement);
}

public class EvidenceSupportLevelClassifier : IEvidenceSupportLevelClassifier
{
    public EvidenceSupportLevel ClassifyEvidence(ResearchSource source, string statement)
    {
        return source.SourceType switch
        {
            ResearchSourceType.ExistingInternal => EvidenceSupportLevel.VerifiedClientFact,
            ResearchSourceType.OperatorUploaded => EvidenceSupportLevel.VerifiedExternalSource,
            ResearchSourceType.AgentDiscoveredExternal => EvidenceSupportLevel.ObservedMarketLanguage,
            _ => EvidenceSupportLevel.Unsupported
        };
    }

    public EvidenceSupportLevel GetCeiling(ResearchSourceType sourceType) => ClassifyEvidence(
        new ResearchSource { SourceType = sourceType },
        "");
}
