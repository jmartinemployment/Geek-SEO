namespace GeekSeo.Application.Models.Seo;

public static class InternalLinkTypes
{
    public const string Spoke = "spoke";
    public const string Sibling = "sibling";
    public const string Pillar = "pillar";
}

public sealed record InternalLinkSuggestRequest
{
    public required Guid ProjectId { get; init; }
    public required Guid DocumentId { get; init; }
    public int MaxSuggestions { get; init; } = 10;
}

public sealed record InternalLinkSuggestion
{
    public required Guid TargetDocumentId { get; init; }
    public required string AnchorText { get; init; }
    /// <summary>Blog publish path when available; otherwise editor fallback.</summary>
    public required string TargetUrl { get; init; }
    public string? PublishPath { get; init; }
    public required string LinkType { get; init; }
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
