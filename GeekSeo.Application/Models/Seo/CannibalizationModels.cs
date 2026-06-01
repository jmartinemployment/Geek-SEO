namespace GeekSeo.Application.Models.Seo;

public sealed record CannibalizationPage
{
    public required string Url { get; init; }
    public required long Impressions { get; init; }
    public required long Clicks { get; init; }
    public required double Position { get; init; }
}

public sealed record CannibalizationIssue
{
    public required string Query { get; init; }
    public required IReadOnlyList<CannibalizationPage> Pages { get; init; }
    public required string Severity { get; init; }
    public required string Recommendation { get; init; }
    public required long TotalImpressions { get; init; }
}

public sealed record CannibalizationReport
{
    public required Guid ProjectId { get; init; }
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public required int GscRowCount { get; init; }
    public required int UniqueQueryCount { get; init; }
    public required int MultiUrlQueryCount { get; init; }
    public required int CompetingQueryCount { get; init; }
    public required IReadOnlyList<CannibalizationIssue> Issues { get; init; }
}
