namespace GeekSeo.Application.Models.Seo;

/// <summary>Small PATCH replacing part of fat analysis-results.</summary>
public sealed record SiteAnalysisProfileSummaryPatch(
    string PrimaryFocus,
    string FocusDescription,
    string[] FocusTags,
    string AudienceType,
    int TotalPillarsIdentified,
    DateTimeOffset AnalyzedAt,
    DateTimeOffset NextAnalysisDue,
    string? ScanFingerprint = null,
    decimal? ScanChangeScore = null,
    string? PersistStage = null,
    string? StructureStatus = null,
    string? EnrichmentStatus = null);

public sealed record SiteAnalysisTopicCandidateBulkUpsert(
    Guid? Id,
    Guid SiteAnalysisProfileId,
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

public sealed record SiteAnalysisTopicCandidatePage(
    Guid Id,
    Guid SiteAnalysisProfileId,
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

public sealed record SiteAnalysisTopicCandidateListResult(
    IReadOnlyList<SiteAnalysisTopicCandidatePage> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record SiteAnalysisProfileStepRunUpsert(
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

public sealed record SiteAnalysisProfileStepRunStatusPatch(
    string Status,
    DateTimeOffset? HeartbeatAt = null,
    DateTimeOffset? CompletedAt = null,
    string? ErrorMessage = null,
    int? RetryCount = null,
    string? Summary = null);

public sealed record SiteAnalysisProfileStepRunRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
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

public sealed record SiteAnalysisProfileSchemaSignalWrite(
    string SchemaType,
    string PropertyName,
    string PropertyValue,
    string? SourceUrl,
    int DisplayOrder = 0);

public sealed record SiteAnalysisProfileSchemaSignalRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string SchemaType,
    string PropertyName,
    string PropertyValue,
    string? SourceUrl,
    int DisplayOrder);

public sealed record SiteAnalysisProfileDiscoveredUrlWrite(
    string Url,
    string SourceType,
    DateTimeOffset? LastSeenAt = null);

public sealed record SiteAnalysisProfileDiscoveredUrlRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string Url,
    string SourceType,
    DateTimeOffset? LastSeenAt);

public sealed record SiteAnalysisProfileNavigationLinkWrite(
    string? SourceUrl,
    string LinkUrl,
    string? AnchorText,
    string? LinkArea,
    int DisplayOrder = 0);

public sealed record SiteAnalysisProfileNavigationLinkRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string? SourceUrl,
    string LinkUrl,
    string? AnchorText,
    string? LinkArea,
    int DisplayOrder);

public sealed record SiteAnalysisProfileHeadingWrite(
    string PageUrl,
    int HeadingLevel,
    string HeadingText,
    int DisplayOrder = 0);

public sealed record SiteAnalysisProfileHeadingRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string PageUrl,
    int HeadingLevel,
    string HeadingText,
    int DisplayOrder);

/// <summary>One crawled page's real heading+paragraph tree, JSON-serialized
/// (<c>IReadOnlyList&lt;PageSection&gt;</c>) — one row per page, replacing the flat
/// per-heading rows above.</summary>
public sealed record SiteAnalysisPageSectionTreeWrite(
    string PageUrl,
    string TreeJson);

public sealed record SiteAnalysisPageSectionTreeRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string PageUrl,
    string TreeJson,
    DateTimeOffset CreatedAtUtc);

public sealed record SiteAnalysisTopicCandidateEvidenceWrite(
    Guid TopicCandidateId,
    string EvidenceType,
    string? SourceUrl,
    string? SourceLabel,
    string EvidenceText,
    int DisplayOrder = 0);

public sealed record SiteAnalysisTopicCandidateEvidenceRow(
    Guid Id,
    Guid TopicCandidateId,
    string EvidenceType,
    string? SourceUrl,
    string? SourceLabel,
    string EvidenceText,
    int DisplayOrder);

public sealed record SiteAnalysisProfilePageContentItemWrite(
    string PageUrl,
    string ItemKind,
    string ItemText,
    int DisplayOrder = 0);

public sealed record SiteAnalysisProfilePageContentItemRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string PageUrl,
    string ItemKind,
    string ItemText,
    int DisplayOrder);

public sealed record SiteAnalysisProfilePageContentWrite(
    string PageUrl,
    int ListItemsScanned,
    IReadOnlyList<SiteAnalysisProfilePageContentItemWrite> Items);

public sealed record SiteAnalysisProfilePageContentRow(
    string PageUrl,
    int ListItemsScanned,
    IReadOnlyList<SiteAnalysisProfilePageContentItemRow> Items);

public sealed record SiteAnalysisProfileSitePageWrite(
    string Url,
    string FetchMethod,
    string VisibleText,
    int WordCount,
    int DisplayOrder = 0);

public sealed record SiteAnalysisProfileSitePageRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string Url,
    string FetchMethod,
    string VisibleText,
    int WordCount,
    int DisplayOrder);

public sealed record SiteAnalysisProfileSitePageLinkWrite(
    string SourceUrl,
    string TargetUrl,
    string AnchorText,
    bool InferredFromUrlSlug,
    int DisplayOrder = 0);

public sealed record SiteAnalysisProfileSitePageLinkRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string SourceUrl,
    string TargetUrl,
    string AnchorText,
    bool InferredFromUrlSlug,
    int DisplayOrder);

public sealed record SiteAnalysisProfileUrlPatternTopicWrite(
    string Name,
    string Slug,
    string Url,
    string PathSegment,
    int DisplayOrder = 0);

public sealed record SiteAnalysisProfileUrlPatternTopicRow(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string Name,
    string Slug,
    string Url,
    string PathSegment,
    int DisplayOrder);

public sealed record SiteAnalysisProfileSiteCrawlMetaWrite(
    int PagesAttempted,
    int PagesFetched);

public sealed record SiteAnalysisProfileSiteCrawlMetaRow(
    Guid SiteAnalysisProfileId,
    int PagesAttempted,
    int PagesFetched);

public sealed record SiteAnalysisProfileSiteStructureWrite(
    IReadOnlyList<SiteAnalysisProfileSitePageWrite> Pages,
    IReadOnlyList<SiteAnalysisProfileSitePageLinkWrite> Links,
    IReadOnlyList<SiteAnalysisProfileUrlPatternTopicWrite> UrlPatterns,
    SiteAnalysisProfileSiteCrawlMetaWrite CrawlMeta);

public sealed record SiteAnalysisProfileSiteStructureRow(
    IReadOnlyList<SiteAnalysisProfileSitePageRow> Pages,
    IReadOnlyList<SiteAnalysisProfileSitePageLinkRow> Links,
    IReadOnlyList<SiteAnalysisProfileUrlPatternTopicRow> UrlPatterns,
    SiteAnalysisProfileSiteCrawlMetaRow? CrawlMeta);

public sealed record SiteAnalysisPhaseStatusPatch(
    string? StructureStatus = null,
    string? EnrichmentStatus = null,
    string? PersistStage = null,
    string? Status = null);

/// <summary>Lightweight status poll — no pillar graph or fusion snapshot.</summary>
public sealed record SiteAnalysisProfileStatusRow(
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
public sealed record SiteAnalysisDetailsRow(
    string Status,
    int AnalysisStepLogVersion,
    string AnalysisStepLog,
    string? FusionSnapshot);
