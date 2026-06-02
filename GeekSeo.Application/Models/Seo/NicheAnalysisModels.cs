namespace GeekSeo.Application.Models.Seo;

// --- Status ---------------------------------------------------------------

public record NicheAnalysisStatus(
    Guid ProfileId,
    string Status,
    string? Step,
    int StepNumber,
    int TotalSteps,
    string? ErrorMessage);

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
    public string DiscoveryMethod { get; init; } = "fallback";
    public decimal TopicalAuthorityScore { get; init; }
    public int TotalPillarsIdentified { get; init; }
    public int PillarsCovered { get; init; }
    public int PillarsPartial { get; init; }
    public int PillarsGap { get; init; }
    public DateTimeOffset? AnalyzedAt { get; init; }
    public DateTimeOffset? NextAnalysisDue { get; init; }
    public string Status { get; init; } = "complete";
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
}

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
    public IReadOnlyList<string> ChildSlugs { get; init; } = [];
}

public record SchemaOrgData(
    IReadOnlyList<string> ServiceNames,
    string? Description,
    string? BrandName,
    IReadOnlyList<string> AreaServed);

public record SitemapData(
    IReadOnlyList<DiscoveredPillar> Pillars,
    int TotalUrlsScanned);

public record NavMenuData(
    IReadOnlyList<DiscoveredPillar> Pillars,
    string ExtractMethod);

public record HomepageHeadings(
    string? Title,
    string? MetaDescription,
    IReadOnlyList<string> H2Texts);
