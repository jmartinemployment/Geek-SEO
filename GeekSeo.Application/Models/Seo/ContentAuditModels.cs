namespace GeekSeo.Application.Models.Seo;

public sealed record PublishedPageMetrics
{
    public required string Url { get; init; }
    public required long RecentClicks { get; init; }
    public required long BaselineClicks { get; init; }
    public required long RecentImpressions { get; init; }
    public required long BaselineImpressions { get; init; }
    public required double RecentPosition { get; init; }
    public required double BaselinePosition { get; init; }
    public required double ClicksChangePercent { get; init; }
    public required double PositionChange { get; init; }
    public required string Status { get; init; }
    public required string Recommendation { get; init; }
}

public sealed record PublishedContentAuditReport
{
    public required Guid ProjectId { get; init; }
    public required string RecentStartDate { get; init; }
    public required string RecentEndDate { get; init; }
    public required string BaselineStartDate { get; init; }
    public required string BaselineEndDate { get; init; }
    public required IReadOnlyList<PublishedPageMetrics> Pages { get; init; }
    public required int DecayingCount { get; init; }
}
