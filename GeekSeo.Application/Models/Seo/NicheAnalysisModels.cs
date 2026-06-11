namespace GeekSeo.Application.Models.Seo;

// --- Status ---------------------------------------------------------------

public record NicheAnalysisStatus(
    Guid ProfileId,
    string Status,
    string? Step,
    int StepNumber,
    int TotalSteps,
    string? ErrorMessage,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? ProgressAt = null,
    string? StructureStatus = null,
    string? EnrichmentStatus = null,
    string? PersistStage = null);

/// <summary>One persisted row in <c>niche_profiles.AnalysisStepLog</c> JSON array.</summary>
public sealed record NicheAnalysisStepLogEntry(
    int StepNumber,
    string Slug,
    string Title,
    string Status,
    string Summary,
    IReadOnlyDictionary<string, object?> Outputs);

public sealed record NicheAnalysisDetails(
    int StepLogVersion,
    IReadOnlyList<NicheAnalysisStepLogEntry> Steps,
    SiteTopicProfile? FusionSnapshot = null);

/// <summary>Persisted when a niche analysis run completes (metadata + scores).</summary>
public sealed record NicheAnalysisSaveRequest(
    string PrimaryNiche,
    string NicheDescription,
    string[] NicheTags,
    string AudienceType,
    /// <summary>Deprecated DB column; GeekRepository still requires the field on PATCH analysis-results.</summary>
    string DiscoveryMethod,
    decimal AuthorityScore,
    int TotalPillarsIdentified,
    int Covered,
    int Partial,
    int Gap,
    DateTimeOffset AnalyzedAt,
    DateTimeOffset NextAnalysisDue,
    string? FusionSnapshot = null);

/// <summary>Queued niche analysis job with owning user (from seo_projects).</summary>
public record NicheQueuedJob(
    Guid ProfileId,
    Guid ProjectId,
    Guid UserId,
    string Domain);

// --- Result (full profile returned to API callers) ------------------------

public record NicheProfileResult
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Domain { get; init; }
    public required string PrimaryNiche { get; init; }
    public string NicheDescription { get; init; } = string.Empty;
    public string[] NicheTags { get; init; } = [];
    public string AudienceType { get; init; } = "local_service";
    public string CompetitionLevel { get; init; } = "medium";
    public decimal TopicalAuthorityScore { get; init; }
    public int TotalPillarsIdentified { get; init; }
    public int PillarsCovered { get; init; }
    public int PillarsPartial { get; init; }
    public int PillarsGap { get; init; }
    public DateTimeOffset? AnalyzedAt { get; init; }
    public DateTimeOffset? NextAnalysisDue { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string Status { get; init; } = "complete";
    public string? StructureStatus { get; init; }
    public string? EnrichmentStatus { get; init; }
    public IReadOnlyList<NichePillarResult> Pillars { get; init; } = [];
    public IReadOnlyList<NicheCompetitorResult> Competitors { get; init; } = [];
    public IReadOnlyList<NicheEntityResult> Entities { get; init; } = [];
}

public record NichePillarResult
{
    public required Guid Id { get; init; }
    public required string PillarTopic { get; init; }
    public required string PillarSlug { get; init; }
    public required string PrimaryKeyword { get; init; }
    public string? PageUrl { get; init; }
    public string SearchIntent { get; init; } = "commercial";
    public int SearchVolume { get; init; }
    public decimal KeywordDifficulty { get; init; }
    public string CoverageStatus { get; init; } = "gap";
    public decimal CoverageScore { get; init; }
    public int ExistingPageCount { get; init; }
    public int RequiredSubtopicCount { get; init; }
    public int CoveredSubtopicCount { get; init; }
    public string StrategicPriority { get; init; } = "expansion";
    public string? ContentAngle { get; init; }
    public string Source { get; init; } = "sitemap";
    public int DisplayOrder { get; init; }
    public IReadOnlyList<NicheSubtopicResult> Subtopics { get; init; } = [];
    public IReadOnlyList<PaaQuestionItem> PaaQuestions { get; init; } = [];
    public IReadOnlyList<string> RelatedSearches { get; init; } = [];
}

public record PaaQuestionItem(string Question, string? Answer, string? SourceUrl, string? SourceTitle);

public record NicheSubtopicResult
{
    public required Guid Id { get; init; }
    public required string SubtopicTitle { get; init; }
    public required string TargetKeyword { get; init; }
    public string SearchIntent { get; init; } = "informational";
    public int SearchVolume { get; init; }
    public decimal KeywordDifficulty { get; init; }
    public string CoverageStatus { get; init; } = "gap";
    public string? ExistingUrl { get; init; }
    public string RecommendedFormat { get; init; } = "how_to";
    public int RecommendedWordCount { get; init; }
    public string FixEffort { get; init; } = "create";
    public bool IsQuickWin { get; init; }
}

public record NicheCompetitorResult(
    Guid Id, string Domain, int SerpPresence,
    decimal EstimatedAuthorityScore, int PillarsRanking, string StrengthAssessment);

public record NicheEntityResult(
    Guid Id, string EntityName, string EntityType,
    int MentionFrequency, bool PresentOnDomain, Guid[] AssociatedPillarIds);

// --- Dapper read models ---------------------------------------------------

public record NicheProfileSummary(
    Guid Id, string Domain, string PrimaryNiche,
    decimal TopicalAuthorityScore, int TotalPillars,
    int PillarsCovered, int PillarsGap,
    string CompetitionLevel, DateTimeOffset? AnalyzedAt, string Status);

public record PillarCoverageMatrix(
    Guid PillarId, string PillarTopic, string PrimaryKeyword,
    int SearchVolume, decimal KeywordDifficulty, decimal CoverageScore,
    int CoveredSubtopics, int TotalSubtopics, int GapSubtopics,
    string CoverageStatus, string StrategicPriority, bool HasQuickWins);

public record TopicalGapSummary(
    Guid SubtopicId, string PillarTopic, string SubtopicTitle,
    string TargetKeyword, int SearchVolume, decimal KeywordDifficulty,
    bool IsQuickWin, string RecommendedFormat, string FixEffort);

public record AuthorityProgressPoint(
    DateTimeOffset SnapshotDate, decimal TopicalAuthorityScore,
    int PillarsCovered, int TotalSubtopicsCovered, int TotalGaps);

public record CompetitorNicheOverlap(
    string CompetitorDomain, int SharedPillarCount,
    int CompetitorOnlyPillarCount, int OurOnlyPillarCount,
    decimal EstimatedAuthorityScore);

public record EntityCoverageReport(
    string EntityName, string EntityType,
    int MentionFrequency, bool PresentOnDomain, int AssociatedPillarCount);

// --- Internal extraction types --------------------------------------------

public record DiscoveredPillar
{
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public string? PageUrl { get; init; }
    public string Intent { get; init; } = "commercial";
    public string Source { get; init; } = "sitemap";
    public int ChildPageCount { get; init; }
    /// <summary>Mirrors <see cref="TopicCandidate.ContentDepthScore"/>; 0 when created outside fusion path.</summary>
    public decimal ContentDepthScore { get; init; }
    public IReadOnlyList<string> ChildSlugs { get; init; } = [];
}

public record SchemaOrgData(
    IReadOnlyList<string> ServiceNames,
    IReadOnlyList<string> KnowsAboutTopics,
    IReadOnlyList<string> OfferCatalogTopics,
    string? Description,
    string? BrandName,
    IReadOnlyList<string> AreaServed,
    IReadOnlyList<string> SameAsUrls,
    IReadOnlyList<string> ResolvedEntityPlatforms,
    bool EntityResolved);

public sealed record PillarMergeResult(
    IReadOnlyList<DiscoveredPillar> Selected,
    IReadOnlyList<DiscoveredPillar> Excluded);

/// <summary>One normalized topic phrase before pillar selection (Search Understanding Layer).</summary>
public sealed record TopicCandidate
{
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required IReadOnlyList<TopicEvidence> Evidence { get; init; }
    public decimal Confidence { get; init; }
    /// <summary>Composite signal: dedicated URL, internal links, and content-zone evidence (0–1).</summary>
    public decimal ContentDepthScore { get; init; }
    public string? DedicatedPageUrl { get; init; }
    public int InternalLinkCount { get; init; }
}

public sealed record TopicEvidence
{
    public required string Source { get; init; }
    public string? Snippet { get; init; }
    public string? Url { get; init; }
    public decimal Weight { get; init; }
}

public sealed record SiteTopicProfile
{
    public required IReadOnlyList<TopicCandidate> AllCandidates { get; init; }
    public required IReadOnlyList<TopicCandidate> SelectedPillars { get; init; }
    public required IReadOnlyList<TopicCandidate> ExcludedCandidates { get; init; }
    public required IReadOnlyDictionary<string, string> ExclusionReasons { get; init; }
    public required string SulVersion { get; init; }
    public required IReadOnlyList<string> SignalSourcesPresent { get; init; }
    /// <summary>Share of crawled site word-weight attributed to each selected pillar slug (0–1).</summary>
    public IReadOnlyDictionary<string, decimal> NormalizedTopicalityBySlug { get; init; }
        = new Dictionary<string, decimal>();
    /// <summary>SERP-derived entity coverage per selected pillar slug (Gap 3).</summary>
    public IReadOnlyDictionary<string, PillarEntityCoverage> EntityCoverageBySlug { get; init; }
        = new Dictionary<string, PillarEntityCoverage>();
    /// <summary>Pillar-to-pillar internal link graph from crawled anchors (Gap 5).</summary>
    public InternalLinkGraph? InternalLinkGraph { get; init; }
    /// <summary>Phase E draft actions derived from the same snapshot (no auto-execution).</summary>
    public IReadOnlyList<PillarRecommendedAction> RecommendedActions { get; init; } = [];
    /// <summary>Step 11 — schema areaServed vs location pages on site.</summary>
    public LocalGeographyAnalysis? LocalGeography { get; init; }
}

/// <summary>Compares declared service areas to location landing pages on the site.</summary>
public sealed record LocalGeographyAnalysis(
    IReadOnlyList<string> AreasServed,
    IReadOnlyList<LocalLocationPage> LocationPagesFound,
    IReadOnlyList<LocalGeographyGap> Gaps,
    bool IsLocalBusiness);

public sealed record LocalLocationPage(
    string Name,
    string Slug,
    string Url,
    string MatchSource);

public sealed record LocalGeographyGap(
    string AreaName,
    string SuggestedSlug,
    string SuggestedTitle,
    string Reason);

/// <summary>How well the site covers SERP-expected topic entities for one pillar.</summary>
public sealed record PillarEntityCoverage(
    string Slug,
    string Name,
    decimal CoverageScore,
    int ExpectedEntityCount,
    int MatchedEntityCount,
    IReadOnlyList<string> MissingEntities,
    bool IsEntityThin);

public sealed record InternalLinkGraphEdge(
    string FromSlug,
    string ToSlug,
    int LinkCount,
    IReadOnlyList<string> SampleAnchors);

public sealed record InternalLinkGraph(
    IReadOnlyList<InternalLinkGraphEdge> Edges,
    IReadOnlyList<string> OrphanSlugs);

/// <summary>User-approvable action suggested from fusion snapshot analysis (Phase E).</summary>
public sealed record PillarRecommendedAction(
    string ActionType,
    string TopicSlug,
    string TopicName,
    string Summary,
    decimal Priority);

public sealed record PageContentData(
    IReadOnlyList<string> ServicePhrases,
    IReadOnlyList<string> VerticalTopics,
    int ListItemsScanned);

public sealed record CrawledPage(string Url, string Html, string FetchMethod = "http");

/// <summary>Bounded same-origin crawl for structure-signal extractors (Phase B).</summary>
public sealed record SiteCrawlData(
    IReadOnlyList<CrawledPage> Pages,
    int PagesAttempted,
    int PagesFetched);

public sealed record InternalLinkEdge(
    string SourceUrl,
    string TargetUrl,
    string AnchorText,
    bool InferredFromUrlSlug = false);

public sealed record InternalLinkData(
    IReadOnlyList<InternalLinkEdge> Links,
    IReadOnlyDictionary<string, int> InboundCountByTargetUrl,
    int PagesScanned);

public sealed record UrlPatternTopic(
    string Name,
    string Slug,
    string Url,
    string PathSegment);

public sealed record UrlPatternData(
    IReadOnlyList<UrlPatternTopic> Topics,
    int UrlsScanned);

public record SitemapData(
    IReadOnlyList<DiscoveredPillar> Pillars,
    int TotalUrlsScanned,
    IReadOnlyList<string> SampleUrls);

public record NavMenuData(
    IReadOnlyList<DiscoveredPillar> Pillars,
    string ExtractMethod);

public record HomepageHeadings
{
    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    public IReadOnlyList<PageHeading> Headings { get; init; } = [];
    public IReadOnlyList<string> H2Texts { get; init; } = [];
}

// --- Bulk insert DTOs (no EF navigation properties — safe for JSON APIs) ----

public sealed record NichePillarBulkInsert(
    Guid Id,
    Guid NicheProfileId,
    string PillarTopic,
    string PillarSlug,
    string PrimaryKeyword,
    string? PageUrl,
    string SearchIntent,
    int SearchVolume,
    decimal KeywordDifficulty,
    string CoverageStatus,
    decimal CoverageScore,
    int ExistingPageCount,
    int RequiredSubtopicCount,
    int CoveredSubtopicCount,
    int Priority,
    string StrategicPriority,
    string? ContentAngle,
    decimal EstimatedTrafficPotential,
    string Source,
    int DisplayOrder,
    DateTimeOffset? CreatedAt = null);

public sealed record NicheSubtopicBulkInsert(
    Guid Id,
    Guid PillarId,
    string SubtopicTitle,
    string TargetKeyword,
    string SearchIntent,
    int SearchVolume,
    decimal KeywordDifficulty,
    string CoverageStatus,
    string? ExistingUrl,
    string RecommendedFormat,
    int RecommendedWordCount,
    string FixEffort,
    bool IsQuickWin,
    DateTimeOffset? CreatedAt = null);

public sealed record NicheCompetitorBulkInsert(
    Guid Id,
    Guid NicheProfileId,
    string Domain,
    int SerpPresence,
    decimal EstimatedAuthorityScore,
    int PillarsRanking,
    string StrengthAssessment);

public sealed record NicheEntityBulkInsert(
    Guid Id,
    Guid NicheProfileId,
    string EntityName,
    string EntityType,
    int MentionFrequency,
    bool PresentOnDomain,
    Guid[] AssociatedPillarIds);

public sealed record NichePillarPageBulkInsert(
    Guid Id,
    Guid PillarId,
    string Url,
    string? PageTitle,
    int WordCount,
    string CoverageQuality,
    decimal RelevanceScore,
    string[] TopicsFound,
    string[] GapsFound);
