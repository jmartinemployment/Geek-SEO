namespace SiteAnalyzer2.Services.CompetitorCrawl;

public sealed record CompetitorOverviewLiteDto
{
    public required Guid RunId { get; init; }
    public required string Keyword { get; init; }
    public int MaxDomains { get; init; } = CompetitorOverviewLiteService.MaxDomains;
    public required IReadOnlyList<CompetitorOverviewDomainRowDto> Domains { get; init; }
    public bool Analyzed { get; init; }
    public bool CompetitorSaved { get; init; }
    public string? Message { get; init; }
}

public sealed record CompetitorOverviewDomainRowDto
{
    public int SerpRank { get; init; }
    public required string Domain { get; init; }
    public required string Url { get; init; }
    public string? Title { get; init; }
    public string? Snippet { get; init; }
    public int? H2Count { get; init; }
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];
    public int? HttpStatus { get; init; }
    /// <summary>serp_only | fetched | failed</summary>
    public required string FetchStatus { get; init; }
}

public sealed record CompetitorOverviewLiteRunOutcome
{
    public required bool CompetitorSaved { get; init; }
    public int DomainCount { get; init; }
    public int PagesFetched { get; init; }
    public required CompetitorOverviewLiteDto Overview { get; init; }
    public string? Message { get; init; }
}
