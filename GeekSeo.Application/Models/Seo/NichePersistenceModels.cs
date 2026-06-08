namespace GeekSeo.Application.Models.Seo;

/// <summary>Small PATCH replacing part of fat analysis-results.</summary>
public sealed record NicheProfileSummaryPatch(
    string PrimaryNiche,
    string NicheDescription,
    string[] NicheTags,
    string AudienceType,
    int TotalPillarsIdentified,
    DateTimeOffset AnalyzedAt,
    DateTimeOffset NextAnalysisDue,
    string? ScanFingerprint = null,
    decimal? ScanChangeScore = null,
    string? PersistStage = null,
    string? StructureStatus = null,
    string? EnrichmentStatus = null);

public sealed record NicheTopicCandidateBulkUpsert(
    Guid? Id,
    Guid NicheProfileId,
    string Slug,
    string Name,
    decimal Confidence,
    bool IsSelected,
    string? ExclusionReason,
    string? DedicatedPageUrl,
    int InternalLinkCount,
    decimal ContentDepthScore,
    int DisplayOrder,
    string? EvidenceJson);

public sealed record NicheTopicCandidatePage(
    Guid Id,
    Guid NicheProfileId,
    string Slug,
    string Name,
    decimal Confidence,
    bool IsSelected,
    string? ExclusionReason,
    string? DedicatedPageUrl,
    int InternalLinkCount,
    decimal ContentDepthScore,
    int DisplayOrder,
    IReadOnlyList<TopicEvidence>? Evidence);

public sealed record NicheTopicCandidateListResult(
    IReadOnlyList<NicheTopicCandidatePage> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record NichePhaseStatusPatch(
    string? StructureStatus = null,
    string? EnrichmentStatus = null,
    string? PersistStage = null,
    string? Status = null);

/// <summary>Lightweight status poll — no pillar graph or fusion snapshot.</summary>
public sealed record NicheProfileStatusRow(
    Guid Id,
    string Status,
    string? AnalysisStep,
    int AnalysisStepNumber,
    int AnalysisTotalSteps,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AnalysisProgressAt,
    string StructureStatus,
    string EnrichmentStatus,
    string? PersistStage);

/// <summary>Lightweight analysis-details read — step log without eager pillar includes.</summary>
public sealed record NicheAnalysisDetailsRow(
    string Status,
    int AnalysisStepLogVersion,
    string AnalysisStepLog,
    string? FusionSnapshot);
