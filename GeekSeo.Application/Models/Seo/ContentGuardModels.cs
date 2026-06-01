namespace GeekSeo.Application.Models.Seo;

public sealed record ContentGuardPolicyDto
{
    public required Guid ProjectId { get; init; }
    public required bool Enabled { get; init; }
    public required bool AutoPatch { get; init; }
}

public sealed record ContentGuardRunDto
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public Guid? DocumentId { get; init; }
    public required string Url { get; init; }
    public required string Status { get; init; }
    public string? Recommendation { get; init; }
    public int? WordPressDraftPostId { get; init; }
    public required string DetectedAt { get; init; }
    public string? CompletedAt { get; init; }
}

public sealed record ContentGuardScanSummary
{
    public int DecayingPagesFound { get; init; }
    public int RunsCreated { get; init; }
    public int PatchesAttempted { get; init; }
    public int PatchesSucceeded { get; init; }
    public int PatchesFailed { get; init; }
}

public sealed record UpsertContentGuardPolicyRequest
{
    public required bool Enabled { get; init; }
    public required bool AutoPatch { get; init; }
}

public sealed record GeoTrackingQueryDto
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string QueryText { get; init; }
    public required IReadOnlyList<string> Platforms { get; init; }
    public required bool Enabled { get; init; }
}

public sealed record CreateGeoTrackingQueryRequest
{
    public required Guid ProjectId { get; init; }
    public required string QueryText { get; init; }
    public IReadOnlyList<string> Platforms { get; init; } = ["google_aio"];
}

public sealed record GeoTrendPoint
{
    public required string Date { get; init; }
    public required string Platform { get; init; }
    public required bool Mentioned { get; init; }
}

public sealed record GeoTrendsResponse
{
    public required Guid QueryId { get; init; }
    public required string QueryText { get; init; }
    public required IReadOnlyList<GeoTrendPoint> Points { get; init; }
    public required double MentionRate30d { get; init; }
}
