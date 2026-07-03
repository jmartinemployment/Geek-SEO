namespace SiteAnalyzer2.Services.Integrations;

using SiteAnalyzer2.Services.Rankings;

public sealed record KeywordPageImportResultDto
{
    public required Guid ProjectId { get; init; }
    public required Guid KeywordProjectId { get; init; }
    public required string Keyword { get; init; }
    public required bool KeywordSaved { get; init; }
    /// <summary>Organic + paid result rows (legacy aggregate).</summary>
    public int OrganicCount { get; init; }
    public int OrganicOnlyCount { get; init; }
    public int PaidCount { get; init; }
    public int AiOverviewCount { get; init; }
    public bool AiOverviewAvailable { get; init; }
    public int PaaCount { get; init; }
    public int CompetitorCrawlSeedCount { get; init; }
    public bool FilterApplied { get; init; }
    public int FilterIncludedCount { get; init; }
    public int FilterExcludedCount { get; init; }
    public int FilterRejectedCount { get; init; }
    public int FilterPendingReviewCount { get; init; }
    public int FilterCrawlEligibleCount { get; init; }
    public string? Message { get; init; }
    public int? TargetOrganicPosition { get; init; }
    public string? TargetOrganicUrl { get; init; }
    public RankingsDeltaDto? RankingsDelta { get; init; }
    public string? TopicSlug { get; init; }
}

public sealed record CompetitorCrawlWorkflowResultDto
{
    public required bool CompetitorSaved { get; init; }
    public int TotalPages { get; init; }
    public int DomainCount { get; init; }
    public IReadOnlyList<string> QualityWarnings { get; init; } = [];
    public string? Message { get; init; }
}
