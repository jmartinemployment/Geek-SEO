namespace GeekSeo.Application.Models.Seo;

public sealed record TopicalMapTopic
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Queries { get; init; }
    public required string Coverage { get; init; }
    public string? MatchedDocumentId { get; init; }
    public string? MatchedDocumentTitle { get; init; }
    public string? MatchedPageUrl { get; init; }
    /// <summary>gsc = live Search Console landing page; document = in-app content draft.</summary>
    public string? MatchSource { get; init; }
    public required long TotalImpressions { get; init; }
}

public sealed record TopicalMapResult
{
    public required Guid ProjectId { get; init; }
    public required string GeneratedAt { get; init; }
    public string? ExpiresAt { get; init; }
    public required IReadOnlyList<TopicalMapTopic> Topics { get; init; }
    public required int CoveredCount { get; init; }
    public required int GapCount { get; init; }
    public required int PartialCount { get; init; }
}
