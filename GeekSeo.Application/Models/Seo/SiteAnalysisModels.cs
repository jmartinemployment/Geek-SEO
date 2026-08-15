namespace GeekSeo.Application.Models.Seo;

// --- Status ---------------------------------------------------------------

public record SiteAnalysisStatus(
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
    string? PersistStage = null,
    IReadOnlyDictionary<string, string>? StepStatuses = null,
    IReadOnlyDictionary<string, string>? StepSummaries = null,
    IReadOnlyDictionary<string, string>? StepErrors = null,
    IReadOnlyDictionary<string, string>? StepWarnings = null);

/// <summary>One persisted row in <c>site_analysis_profiles.AnalysisStepLog</c> JSON array.</summary>
public sealed record SiteAnalysisStepLogEntry(
    int StepNumber,
    string Slug,
    string Title,
    string Status,
    string Summary,
    IReadOnlyDictionary<string, object?> Outputs);

public sealed record SiteAnalysisDetails(
    int StepLogVersion,
    IReadOnlyList<SiteAnalysisStepLogEntry> Steps,
    SiteTopicProfile? FusionSnapshot = null,
    IReadOnlyList<SiteAnalysisStepDefinitionDto>? StepDefinitions = null);

public sealed record SiteAnalysisStepDefinitionDto(
    int StepNumber,
    string Slug,
    string Title,
    string Phase,
    IReadOnlyList<string> Dependencies,
    bool IsOptional,
    bool IsTerminal);

/// <summary>Persisted when a site analysis run completes (metadata + scores).</summary>
public sealed record SiteAnalysisSaveRequest(
    string PrimaryFocus,
    string FocusDescription,
    string[] FocusTags,
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

/// <summary>Queued site analysis job with owning user (from seo_projects).</summary>
public record SiteAnalysisQueuedJob(
    Guid ProfileId,
    Guid ProjectId,
    Guid UserId,
    string Domain);

// --- Result (full profile returned to API callers) ------------------------

public record SiteAnalysisProfileResult
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Domain { get; init; }
    public required string PrimaryFocus { get; init; }
    public string FocusDescription { get; init; } = string.Empty;
    public string[] FocusTags { get; init; } = [];
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
    public IReadOnlyList<SiteAnalysisPillarResult> Pillars { get; init; } = [];
    public IReadOnlyList<SiteCompetitorResult> Competitors { get; init; } = [];
    public IReadOnlyList<SiteAnalysisEntityResult> Entities { get; init; } = [];
}

public record SiteAnalysisPillarResult
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
    public IReadOnlyList<SiteAnalysisSubtopicResult> Subtopics { get; init; } = [];
    public IReadOnlyList<PaaQuestionItem> PaaQuestions { get; init; } = [];
    public IReadOnlyList<string> RelatedSearches { get; init; } = [];
    public IReadOnlyList<PaaQuestionItem> LocalPaaQuestions { get; init; } = [];
    public IReadOnlyList<string> LocalRelatedSearches { get; init; } = [];
}

public record PaaQuestionItem(string Question, string? Answer, string? SourceUrl, string? SourceTitle);

public record CompetitorSiteInsightDto(
    string Domain,
    int PagesCrawled,
    int AvgWordCount,
    IReadOnlyList<string> TopHeadings,
    bool HasFaqSchema,
    string Scope = "national",
    IReadOnlyList<string>? Services = null,
    IReadOnlyList<string>? KnowsAbout = null,
    IReadOnlyList<string>? AreaServed = null,
    IReadOnlyList<string>? SameAs = null,
    string? Description = null,
    string? BrandName = null);

public record SiteAnalysisSubtopicResult
{
    public required Guid Id { get; init; }
    public required string SubtopicTitle { get; init; }
    public required string TargetKeyword { get; init; }
    public string SearchIntent { get; init; } = "informational";
    public int SearchVolume { get; init; }
    public decimal KeywordDifficulty { get; init; }
    public string CoverageStatus { get; init; } = "gap";
    public string? ExistingUrl { get; init; }
    public string RecommendedFormat { get; init; } = "";
    public int RecommendedWordCount { get; init; }
    public string FixEffort { get; init; } = "create";
    public bool IsQuickWin { get; init; }
}

public record SiteCompetitorResult(
    Guid Id, string Domain, int SerpPresence,
    decimal EstimatedAuthorityScore, int PillarsRanking, string StrengthAssessment,
    string Scope = "national",
    int PagesCrawled = 0, int AvgWordCount = 0, bool HasFaqSchema = false,
    IReadOnlyList<string>? Services = null,
    IReadOnlyList<string>? KnowsAbout = null,
    IReadOnlyList<string>? AreaServed = null,
    IReadOnlyList<string>? SameAs = null,
    string? Description = null,
    string? BrandName = null,
    IReadOnlyList<CompetitorPillarResult>? Pillars = null,
    DateTimeOffset? CompetitorAnalyzedAt = null);

public record CompetitorPillarResult(
    string Name,
    string Slug,
    string Source,
    decimal Confidence);

public record SiteAnalysisEntityResult(
    Guid Id, string EntityName, string EntityType,
    int MentionFrequency, bool PresentOnDomain, Guid[] AssociatedPillarIds);

// --- Dapper read models ---------------------------------------------------

public record SiteAnalysisProfileSummary(
    Guid Id, string Domain, string PrimaryFocus,
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

public record CompetitorFocusOverlap(
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

/// <summary>
/// One fetch/render of a URL. This is the indexer seam: URL identity, status, redirects,
/// and directives live here — never a bare HTML string.
/// </summary>
public sealed record CrawledPage(string Url, string Html, string FetchMethod = "http")
{
    /// <summary>response.Url after Playwright follows redirects.</summary>
    public string FinalUrl { get; init; } = "";

    public int StatusCode { get; init; }

    public IReadOnlyList<string> RedirectChain { get; init; } = [];

    /// <summary>rel=canonical hint. Non-canonical pages are kept; consolidation is index-time.</summary>
    public string? Canonical { get; init; }

    /// <summary>
    /// Indexed: no. Crawled and followed: yes. Downstream decides exclusion once an index exists.
    /// </summary>
    public bool NoIndex { get; init; }

    public bool NoFollow { get; init; }

    public bool SoftNotFound { get; init; }

    public DateTimeOffset FetchedAt { get; init; }

    /// <summary>Anchors discovered from the rendered DOM (href already absolutized when possible).</summary>
    public IReadOnlyList<DiscoveredCrawlLink> Links { get; init; } = [];

    public string? ContentType { get; init; }
}

public sealed record DiscoveredCrawlLink(string Href, string Text, string Rel = "");

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
    /// <summary>Document title when extracted from a single page (homepage step).</summary>
    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    /// <summary>
    /// Heading list for pillar candidates. After site crawl this is every crawled page
    /// (not homepage-only); the type name is historical.
    /// </summary>
    public IReadOnlyList<PageHeading> Headings { get; init; } = [];
    public IReadOnlyList<string> H2Texts { get; init; } = [];
}

// --- Bulk insert DTOs (no EF navigation properties — safe for JSON APIs) ----

public sealed record SiteAnalysisPillarBulkInsert(
    Guid Id,
    Guid SiteAnalysisProfileId,
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

public sealed record SiteAnalysisSubtopicBulkInsert(
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

public sealed record SiteAnalysisCompetitorBulkInsert(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string Domain,
    int SerpPresence,
    decimal EstimatedAuthorityScore,
    int PillarsRanking,
    string StrengthAssessment,
    string Scope = "national",
    int PagesCrawled = 0,
    int AvgWordCount = 0,
    bool HasFaqSchema = false,
    string? ServicesJson = null,
    string? KnowsAboutJson = null,
    string? AreaServedJson = null,
    string? SameAsJson = null,
    string? Description = null,
    string? BrandName = null,
    string? PillarsJson = null,
    DateTimeOffset? CompetitorAnalyzedAt = null);

public sealed record SiteAnalysisEntityBulkInsert(
    Guid Id,
    Guid SiteAnalysisProfileId,
    string EntityName,
    string EntityType,
    int MentionFrequency,
    bool PresentOnDomain,
    Guid[] AssociatedPillarIds);

public sealed record SiteAnalysisPillarPageBulkInsert(
    Guid Id,
    Guid PillarId,
    string Url,
    string? PageTitle,
    int WordCount,
    string CoverageQuality,
    decimal RelevanceScore,
    string[] TopicsFound,
    string[] GapsFound);
