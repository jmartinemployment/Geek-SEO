namespace GeekSeo.Application.Models.Seo;

public sealed record AnalysisRunSummary
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Keyword { get; init; }
    public string TargetSiteUrl { get; init; } = string.Empty;
    public required string Status { get; init; }
    public long SerpSeResultsCount { get; init; }
    public int OrganicResultCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool ContentWritingReady { get; init; }
}
