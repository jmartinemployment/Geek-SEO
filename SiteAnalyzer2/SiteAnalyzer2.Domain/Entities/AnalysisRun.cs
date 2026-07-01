using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

public class AnalysisRun
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string TargetSiteUrl { get; set; } = string.Empty;
    public string SerpProviderKey { get; set; } = "manual-html";
    /// <summary><see cref="ResearchModes.Manual"/> or <see cref="ResearchModes.Sa2"/>.</summary>
    public string ResearchMode { get; set; } = ResearchModes.Sa2;
    /// <summary>Operator topic folder slug (e.g. customer-journey). One run = one topic.</summary>
    public string? TopicSlug { get; set; }
    public RunStatus Status { get; set; } = RunStatus.Running;
    public PipelineStage? CurrentStage { get; set; }
    public bool IncludeReferenceDomains { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SerpClaimedAt { get; set; }
    public int SerpMaxPage { get; set; } = 1;

    public int? SerpLocationCode { get; set; } = 2840;
    public string SerpLanguageCode { get; set; } = "en";
    public string SerpDevice { get; set; } = "desktop";
    public string SerpOs { get; set; } = "windows";
    public int SerpDepth { get; set; }
    public string SerpSeDomain { get; set; } = "google.com";
    public string? SerpCheckUrl { get; set; }
    public DateTime? SerpCapturedAt { get; set; }
    public string? SerpSpellJson { get; set; }
    public string? SerpRefinementChipsJson { get; set; }
    public string? SerpItemTypesJson { get; set; }
    public long? SerpSeResultsCount { get; set; }
    public int SerpPagesCount { get; set; } = 1;
    public int SerpItemsCount { get; set; }
    public bool SerpLocalPackPresent { get; set; }
    public bool SerpShoppingResultsPresent { get; set; }

    // Run-level writing focus fields
    public string? MatchedPillarTopic { get; set; }
    public string? MatchedPillarIntent { get; set; }
    public string? MatchedPillarAngle { get; set; }
    public List<string> GapTopics { get; set; } = [];
    public string? WritingInstructions { get; set; }

    public string? CompetitorCrawlStatus { get; set; }
    public string? CompetitorCrawlMessage { get; set; }
    public DateTime? CompetitorCrawlStartedAt { get; set; }
    public DateTime? CompetitorCrawlFinishedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<SerpItem> SerpItems { get; set; } = [];
    public ICollection<Page> Pages { get; set; } = [];
    public ICollection<ComparisonCheck> ComparisonChecks { get; set; } = [];
    public ICollection<Finding> Findings { get; set; } = [];
    public ICollection<RunGate> RunGates { get; set; } = [];
}
