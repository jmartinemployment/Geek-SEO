namespace SiteAnalyzer2.Services.CompetitorCrawl;

/// <summary>
/// Organic Research / Domain Overview — positions from owned SERP imports + optional page fetch.
/// </summary>
public sealed record DomainOverviewDto
{
    public required string Domain { get; init; }
    public required string SiteRootUrl { get; init; }
    public required string AnalyzedUrl { get; init; }
    public required string RequestedInput { get; init; }
    public required string Scope { get; init; }
    public bool PageFetched { get; init; }
    public string? PageTitle { get; init; }
    public string? MetaDescription { get; init; }
    public int? H2Count { get; init; }
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];
    public int? HttpStatus { get; init; }
    /// <summary>Distinct keywords from owned SERP imports where this domain appeared in organic.</summary>
    public int? OrganicKeywordsCount { get; init; }
    public int? OrganicTrafficEstimate { get; init; }
    public int? OrganicTrafficCost { get; init; }
    public int? AuthorityScore { get; init; }
    public int? ReferringDomainsCount { get; init; }
    public double? KeywordsChangePercent { get; init; }
    public double? TrafficChangePercent { get; init; }
    public double? TrafficCostChangePercent { get; init; }
    public int TotalPositionsCount { get; init; }
    public int ResearchImportCount { get; init; }
    public IReadOnlyList<DomainOrganicPositionRow> Positions { get; init; } = [];
    public string? Message { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record DomainOrganicPositionRow
{
    public required string Keyword { get; init; }
    public int Position { get; init; }
    public string? Intent { get; init; }
    public int? Traffic { get; init; }
    public double? TrafficPercent { get; init; }
    public int? Volume { get; init; }
    public int? KeywordDifficulty { get; init; }
    public required string Url { get; init; }
    public string? SerpFeatures { get; init; }
    /// <summary>exact | strong | weak — URL path overlap with pillar keyword.</summary>
    public string? PathMatch { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
