namespace GeekSeo.Application.Models.Seo;

public sealed record InternalLinkSuggestRequest
{
    public required Guid ProjectId { get; init; }
    public required Guid DocumentId { get; init; }
    public int MaxSuggestions { get; init; } = 10;
}

public sealed record InternalLinkSuggestion
{
    public required string AnchorText { get; init; }
    public required string TargetUrl { get; init; }
    public required string Reason { get; init; }
    public required double RelevanceScore { get; init; }
}

public sealed record InternalLinkAutoInsertRequest
{
    public required Guid ProjectId { get; init; }
    public required Guid DocumentId { get; init; }
}

public sealed record InternalLinkAutoInsertResult
{
    public required bool Inserted { get; init; }
    public required string ContentHtml { get; init; }
    public string? AnchorText { get; init; }
    public string? TargetUrl { get; init; }
    public string? Message { get; init; }
}
