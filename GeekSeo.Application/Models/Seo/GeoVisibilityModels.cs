namespace GeekSeo.Application.Models.Seo;

public sealed record GeoPlatformStatus
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool Configured { get; init; }
    public string? Provider { get; init; }
    public string? Note { get; init; }
}

public sealed record GeoPlatformsResponse
{
    public required IReadOnlyList<GeoPlatformStatus> Platforms { get; init; }
}

public sealed record GeoProbeRequest
{
    public required Guid ProjectId { get; init; }
    public required string Query { get; init; }
    public string Location { get; init; } = "United States";
}

public sealed record GeoProbeResult
{
    public required Guid ProjectId { get; init; }
    public required string Query { get; init; }
    public required string Platform { get; init; }
    public required bool Mentioned { get; init; }
    public required bool HasAiOverview { get; init; }
    public int? OrganicPosition { get; init; }
    public string? Snippet { get; init; }
    public required string CheckedAt { get; init; }
    public string? Note { get; init; }
}
