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

public sealed record NicheProfileStepRunUpsert(
    int StepNumber,
    string StepSlug,
    string Status,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? HeartbeatAt = null,
    DateTimeOffset? CompletedAt = null,
    string? ErrorMessage = null,
    int RetryCount = 0,
    int InputVersion = 1,
    int OutputVersion = 1,
    string? Summary = null);

public sealed record NicheProfileStepRunStatusPatch(
    string Status,
    DateTimeOffset? HeartbeatAt = null,
    DateTimeOffset? CompletedAt = null,
    string? ErrorMessage = null,
    int? RetryCount = null,
    string? Summary = null);

public sealed record NicheProfileStepRunRow(
    Guid Id,
    Guid NicheProfileId,
    int StepNumber,
    string StepSlug,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? HeartbeatAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage,
    int RetryCount,
    int InputVersion,
    int OutputVersion,
    string? Summary);

public sealed record NicheProfileSchemaSignalWrite(
    string SchemaType,
    string PropertyName,
    string PropertyValue,
    string? SourceUrl,
    int DisplayOrder = 0);

public sealed record NicheProfileSchemaSignalRow(
    Guid Id,
    Guid NicheProfileId,
    string SchemaType,
    string PropertyName,
    string PropertyValue,
    string? SourceUrl,
    int DisplayOrder);

public sealed record NicheProfileDiscoveredUrlWrite(
    string Url,
    string SourceType,
    DateTimeOffset? LastSeenAt = null);

public sealed record NicheProfileDiscoveredUrlRow(
    Guid Id,
    Guid NicheProfileId,
    string Url,
    string SourceType,
    DateTimeOffset? LastSeenAt);

public sealed record NicheProfileNavigationLinkWrite(
    string? SourceUrl,
    string LinkUrl,
    string? AnchorText,
    string? LinkArea,
    int DisplayOrder = 0);

public sealed record NicheProfileNavigationLinkRow(
    Guid Id,
    Guid NicheProfileId,
    string? SourceUrl,
    string LinkUrl,
    string? AnchorText,
    string? LinkArea,
    int DisplayOrder);

public sealed record NicheProfileHeadingWrite(
    string PageUrl,
    int HeadingLevel,
    string HeadingText,
    int DisplayOrder = 0);

public sealed record NicheProfileHeadingRow(
    Guid Id,
    Guid NicheProfileId,
    string PageUrl,
    int HeadingLevel,
    string HeadingText,
    int DisplayOrder);

public sealed record NicheTopicCandidateEvidenceWrite(
    Guid TopicCandidateId,
    string EvidenceType,
    string? SourceUrl,
    string? SourceLabel,
    string EvidenceText,
    int DisplayOrder = 0);

public sealed record NicheTopicCandidateEvidenceRow(
    Guid Id,
    Guid TopicCandidateId,
    string EvidenceType,
    string? SourceUrl,
    string? SourceLabel,
    string EvidenceText,
    int DisplayOrder);

public sealed record NicheProfilePageContentItemWrite(
    string PageUrl,
    string ItemKind,
    string ItemText,
    int DisplayOrder = 0);

public sealed record NicheProfilePageContentItemRow(
    Guid Id,
    Guid NicheProfileId,
    string PageUrl,
    string ItemKind,
    string ItemText,
    int DisplayOrder);

public sealed record NicheProfilePageContentWrite(
    string PageUrl,
    int ListItemsScanned,
    IReadOnlyList<NicheProfilePageContentItemWrite> Items);

public sealed record NicheProfilePageContentRow(
    string PageUrl,
    int ListItemsScanned,
    IReadOnlyList<NicheProfilePageContentItemRow> Items);

public sealed record NicheProfileSitePageWrite(
    string Url,
    string FetchMethod,
    string VisibleText,
    int WordCount,
    int DisplayOrder = 0);

public sealed record NicheProfileSitePageRow(
    Guid Id,
    Guid NicheProfileId,
    string Url,
    string FetchMethod,
    string VisibleText,
    int WordCount,
    int DisplayOrder);

public sealed record NicheProfileSitePageLinkWrite(
    string SourceUrl,
    string TargetUrl,
    string AnchorText,
    bool InferredFromUrlSlug,
    int DisplayOrder = 0);

public sealed record NicheProfileSitePageLinkRow(
    Guid Id,
    Guid NicheProfileId,
    string SourceUrl,
    string TargetUrl,
    string AnchorText,
    bool InferredFromUrlSlug,
    int DisplayOrder);

public sealed record NicheProfileUrlPatternTopicWrite(
    string Name,
    string Slug,
    string Url,
    string PathSegment,
    int DisplayOrder = 0);

public sealed record NicheProfileUrlPatternTopicRow(
    Guid Id,
    Guid NicheProfileId,
    string Name,
    string Slug,
    string Url,
    string PathSegment,
    int DisplayOrder);

public sealed record NicheProfileSiteCrawlMetaWrite(
    int PagesAttempted,
    int PagesFetched);

public sealed record NicheProfileSiteCrawlMetaRow(
    Guid NicheProfileId,
    int PagesAttempted,
    int PagesFetched);

public sealed record NicheProfileSiteStructureWrite(
    IReadOnlyList<NicheProfileSitePageWrite> Pages,
    IReadOnlyList<NicheProfileSitePageLinkWrite> Links,
    IReadOnlyList<NicheProfileUrlPatternTopicWrite> UrlPatterns,
    NicheProfileSiteCrawlMetaWrite CrawlMeta);

public sealed record NicheProfileSiteStructureRow(
    IReadOnlyList<NicheProfileSitePageRow> Pages,
    IReadOnlyList<NicheProfileSitePageLinkRow> Links,
    IReadOnlyList<NicheProfileUrlPatternTopicRow> UrlPatterns,
    NicheProfileSiteCrawlMetaRow? CrawlMeta);

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
    string? PersistStage,
    string? StepStatusesJson = null);

/// <summary>Lightweight analysis-details read — step log without eager pillar includes.</summary>
public sealed record NicheAnalysisDetailsRow(
    string Status,
    int AnalysisStepLogVersion,
    string AnalysisStepLog,
    string? FusionSnapshot);
