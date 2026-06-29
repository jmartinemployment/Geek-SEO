namespace GeekSeo.Application.Models.Seo;

public sealed record CreateContentSpokeRequest
{
    public required string Phrase { get; init; }
    public string SourceType { get; init; } = SpokeSourceTypes.Manual;
    public string? Title { get; init; }
    public string? TargetKeyword { get; init; }
    public string? PublishSlug { get; init; }
}

public sealed record ContentSpokeSummary
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string TargetKeyword { get; init; }
    public string? PublishSlug { get; init; }
    public string? SpokeSourcePhrase { get; init; }
    public string? SpokeSourceType { get; init; }
    public required string Status { get; init; }
    public int WordCount { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
