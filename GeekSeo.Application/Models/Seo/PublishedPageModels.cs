namespace GeekSeo.Application.Models.Seo;

public sealed record PerformanceSnapshotPoint
{
    public required string Date { get; init; }
    public int? Clicks { get; init; }
    public int? Impressions { get; init; }
    public double? Position { get; init; }
}

public sealed record PublishedPageMetrics
{
    public required string Url { get; init; }
    public Guid? PublishedPageId { get; init; }
    public required string Status { get; init; }
    public required double ClicksChangePercent { get; init; }
    public required double PositionChange { get; init; }
    public required long RecentClicks { get; init; }
    public required long RecentImpressions { get; init; }
    public required double RecentPosition { get; init; }
    public required long BaselineClicks { get; init; }
    public required long BaselineImpressions { get; init; }
    public required double BaselinePosition { get; init; }
    public string? Recommendation { get; init; }
    public IReadOnlyList<PerformanceSnapshotPoint> Sparkline { get; init; } = [];
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
