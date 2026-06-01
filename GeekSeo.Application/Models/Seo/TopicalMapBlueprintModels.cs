namespace GeekSeo.Application.Models.Seo;

public sealed record ContentSequenceItem
{
    public required int Order { get; init; }
    public required string TopicId { get; init; }
    public required string TopicName { get; init; }
    public required TopicalTier Tier { get; init; }
    public string? Reason { get; init; }
}

public sealed record LinkGraphEdge
{
    public required string SourceTopicId { get; init; }
    public required string TargetTopicId { get; init; }
    public required string AnchorText { get; init; }
    public string Priority { get; init; } = "medium"; // high | medium | low
}

public sealed record InternalLinkingBlueprint
{
    public required IReadOnlyList<ContentSequenceItem> Sequences { get; init; }
    public required IReadOnlyList<LinkGraphEdge> LinkGraph { get; init; }
}
