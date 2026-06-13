using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GeekSeo.Persistence.Entities;

public sealed class NicheProfile
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string PrimaryNiche { get; set; } = string.Empty;
    public string NicheDescription { get; set; } = string.Empty;
    public string[] NicheTags { get; set; } = [];
    public string AudienceType { get; set; } = "local_service";
    public string CompetitionLevel { get; set; } = "medium";
    /// <summary>Deprecated — no longer written or exposed via API. Column retained until migration.</summary>
    public string DiscoveryMethod { get; set; } = "fallback";
    public decimal TopicalAuthorityScore { get; set; }
    public int TotalPillarsIdentified { get; set; }
    public int PillarsCovered { get; set; }
    public int PillarsPartial { get; set; }
    public int PillarsGap { get; set; }
    public DateTimeOffset? AnalyzedAt { get; set; }
    public DateTimeOffset? NextAnalysisDue { get; set; }
    public string AnalysisVersion { get; set; } = "1.0";
    public string Status { get; set; } = "queued";
    public string? AnalysisStep { get; set; }
    public int AnalysisStepNumber { get; set; }
    public int AnalysisTotalSteps { get; set; } = 10;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AnalysisProgressAt { get; set; }
    public int AnalysisStepLogVersion { get; set; } = 1;
    /// <summary>JSON array of per-step discovery entries (see NicheAnalysisStepLogEntry).</summary>
    public string AnalysisStepLog { get; set; } = "[]";
    /// <summary>Serialized <see cref="GeekSeo.Application.Models.Seo.SiteTopicProfile"/> at analysis completion.</summary>
    public string? FusionSnapshot { get; set; }
    /// <summary>pending | complete | failed</summary>
    public string StructureStatus { get; set; } = "pending";
    /// <summary>pending | enriching | complete | failed | skipped</summary>
    public string EnrichmentStatus { get; set; } = "pending";
    public string? ScanFingerprint { get; set; }
    public decimal? ScanChangeScore { get; set; }
    /// <summary>Resume hint only — DB row counts are source of truth.</summary>
    public string? PersistStage { get; set; }

    [ValidateNever]
    public ICollection<NichePillar> Pillars { get; set; } = [];
    [ValidateNever]
    public ICollection<NicheTopicCandidate> TopicCandidates { get; set; } = [];
    [ValidateNever]
    public ICollection<NicheCompetitor> Competitors { get; set; } = [];
    [ValidateNever]
    public ICollection<NicheEntity> Entities { get; set; } = [];
}

public sealed class NichePillar
{
    public Guid Id { get; set; }
    public Guid NicheProfileId { get; set; }
    public string PillarTopic { get; set; } = string.Empty;
    public string PillarSlug { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public string SearchIntent { get; set; } = "commercial";
    public int SearchVolume { get; set; }
    public decimal KeywordDifficulty { get; set; }
    public string CoverageStatus { get; set; } = "gap";
    public decimal CoverageScore { get; set; }
    public int ExistingPageCount { get; set; }
    public int RequiredSubtopicCount { get; set; }
    public int CoveredSubtopicCount { get; set; }
    public int Priority { get; set; }
    public string StrategicPriority { get; set; } = "expansion";
    public string? ContentAngle { get; set; }
    public decimal EstimatedTrafficPotential { get; set; }
    public string Source { get; set; } = "sitemap";
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CandidateId { get; set; }
    /// <summary>pending | done | skipped | failed</summary>
    public string EnrichmentStatus { get; set; } = "pending";
    public DateTimeOffset? EnrichedAt { get; set; }
    /// <summary>JSON array of {Question, Answer, SourceUrl, SourceTitle} from SERP PAA box.</summary>
    public string PaaQuestionsJson { get; set; } = "[]";
    /// <summary>JSON array of related search strings from SERP (People Also Search For).</summary>
    public string RelatedSearchesJson { get; set; } = "[]";
    /// <summary>PAA from geo-targeted local SERP.</summary>
    public string LocalPaaQuestionsJson { get; set; } = "[]";
    /// <summary>Related searches from geo-targeted local SERP.</summary>
    public string LocalRelatedSearchesJson { get; set; } = "[]";

    [ValidateNever]
    public NicheProfile? NicheProfile { get; set; }
    [ValidateNever]
    public ICollection<NicheSubtopic> Subtopics { get; set; } = [];
    [ValidateNever]
    public ICollection<NichePillarPage> ExistingPages { get; set; } = [];
}

public sealed class NicheSubtopic
{
    public Guid Id { get; set; }
    public Guid PillarId { get; set; }
    public string SubtopicTitle { get; set; } = string.Empty;
    public string TargetKeyword { get; set; } = string.Empty;
    public string SearchIntent { get; set; } = "informational";
    public int SearchVolume { get; set; }
    public decimal KeywordDifficulty { get; set; }
    public string CoverageStatus { get; set; } = "gap";
    public string? ExistingUrl { get; set; }
    public string RecommendedFormat { get; set; } = "how_to";
    public int RecommendedWordCount { get; set; }
    public string FixEffort { get; set; } = "create";
    public bool IsQuickWin { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ValidateNever]
    public NichePillar? Pillar { get; set; }
}

public sealed class NicheCompetitor
{
    public Guid Id { get; set; }
    public Guid NicheProfileId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public int SerpPresence { get; set; }
    public decimal EstimatedAuthorityScore { get; set; }
    public int PillarsRanking { get; set; }
    public string StrengthAssessment { get; set; } = "moderate";
    /// <summary>national | local | both</summary>
    public string Scope { get; set; } = "national";
    // Populated by "Get Competitors" analysis (separate event after initial scan)
    public int PagesCrawled { get; set; }
    public int AvgWordCount { get; set; }
    public bool HasFaqSchema { get; set; }
    public string? ServicesJson { get; set; }
    public string? KnowsAboutJson { get; set; }
    public string? AreaServedJson { get; set; }
    public string? SameAsJson { get; set; }
    public string? Description { get; set; }
    public string? BrandName { get; set; }
    public string? PillarsJson { get; set; }
    public DateTimeOffset? CompetitorAnalyzedAt { get; set; }

    [ValidateNever]
    public NicheProfile? NicheProfile { get; set; }
}

public sealed class NicheEntity
{
    public Guid Id { get; set; }
    public Guid NicheProfileId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int MentionFrequency { get; set; }
    public bool PresentOnDomain { get; set; }
    public Guid[] AssociatedPillarIds { get; set; } = [];

    [ValidateNever]
    public NicheProfile? NicheProfile { get; set; }
}

public sealed class NichePillarPage
{
    public Guid Id { get; set; }
    public Guid PillarId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? PageTitle { get; set; }
    public int WordCount { get; set; }
    public string CoverageQuality { get; set; } = "thin";
    public decimal RelevanceScore { get; set; }
    public string[] TopicsFound { get; set; } = [];
    public string[] GapsFound { get; set; } = [];

    [ValidateNever]
    public NichePillar? Pillar { get; set; }
}

public sealed class NicheTopicCandidate
{
    public Guid Id { get; set; }
    public Guid NicheProfileId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public bool IsSelected { get; set; }
    public string? ExclusionReason { get; set; }
    public string? DedicatedPageUrl { get; set; }
    public int InternalLinkCount { get; set; }
    public decimal ContentDepthScore { get; set; }
    public int DisplayOrder { get; set; }
    /// <summary>Capped JSON array of provenance snippets (max 5 × 120 chars).</summary>
    public string? EvidenceJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ValidateNever]
    public NicheProfile? NicheProfile { get; set; }
}
