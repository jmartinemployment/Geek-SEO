namespace GeekSeo.Application.Models.Seo;

public enum TopicalTier { Pillar, Cluster, Article }

public sealed record QuickWin
{
    public required string TopicName { get; init; }
    public required string Reason { get; init; }
    public string? Intent { get; init; }
    public int? SearchVolume { get; init; }
    public decimal? KeywordDifficulty { get; init; }
}

public sealed record SemanticEntity
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public IReadOnlyList<string> PillarRefs { get; init; } = [];
    public string? Reason { get; init; }
}

public sealed record TopicalMapTopic
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Queries { get; init; }
    /// <summary>covered | partial | gap | opportunity</summary>
    public required string Coverage { get; init; }
    public string? MatchedDocumentId { get; init; }
    public string? MatchedDocumentTitle { get; init; }
    public string? MatchedPageUrl { get; init; }
    /// <summary>gsc = live Search Console landing page; document = in-app content draft.</summary>
    public string? MatchSource { get; init; }
    public required long TotalImpressions { get; init; }
    public string? MainKeyword { get; init; }
    public string? PillarName { get; init; }
    public int? SearchVolume { get; init; }
    public decimal? KeywordDifficulty { get; init; }
    public string? Intent { get; init; }
    public double AveragePosition { get; init; }
    public double PriorityScore { get; init; }
    /// <summary>gsc_page | serp | token</summary>
    public string? ClusterMethod { get; init; }
    public IReadOnlyList<string> CompetitorDomains { get; init; } = [];
    public TopicalTier Tier { get; init; } = TopicalTier.Article;
    public string? PillarId { get; init; }
    public string? ParentClusterId { get; init; }
    public IReadOnlyList<string> EntityGaps { get; init; } = [];
    public decimal EntityCoverage { get; init; } = 0;
    public IReadOnlyList<string> LinkFrom { get; init; } = [];
    public IReadOnlyList<string> LinkTo { get; init; } = [];
    public int? ContentSequence { get; init; }
    public int? SuggestedWordCount { get; init; }
    public string? SuggestedTitle { get; init; }
    public string? SuggestedSlug { get; init; }
    public string? ContentType { get; init; }
    public bool IsDuplicate { get; init; } = false;
    public string? DuplicateOf { get; init; }
    public string? StrategicPriority { get; init; }
}

public sealed record TopicalMapResult
{
    public int Version { get; init; } = 2;
    public required Guid ProjectId { get; init; }
    public required string GeneratedAt { get; init; }
    public string? ExpiresAt { get; init; }
    public required IReadOnlyList<TopicalMapTopic> Topics { get; init; }
    public required int CoveredCount { get; init; }
    public required int GapCount { get; init; }
    public required int PartialCount { get; init; }
    public int OpportunityCount { get; init; }
    public IReadOnlyList<TopicalMapTopic> Recommendations { get; init; } = [];
    public string Mode { get; init; } = "gsc";
    public string? SeedKeyword { get; init; }
    public int PillarCount { get; init; } = 0;
    public int ClusterCount { get; init; } = 0;
    public int ArticleCount { get; init; } = 0;
    public int EntityGapCount { get; init; } = 0;
    public IReadOnlyList<QuickWin> QuickWins { get; init; } = [];
    public IReadOnlyList<SemanticEntity> SemanticEntities { get; init; } = [];
    public int DuplicateCount { get; init; } = 0;
    public InternalLinkingBlueprint? LinkingBlueprint { get; init; }
}
