namespace GeekSeo.Application.Models.Seo;

/// <summary>Atomic child creation payload for legacy BlogSpokeJson migration.</summary>
public sealed record MigrateBlogSpokeChildPayload
{
    public required CreateContentDocumentRequest Child { get; init; }
    public required string ContentHtml { get; init; }
    public required int WordCount { get; init; }
    public required string Status { get; init; }
}

public sealed record ContentBlogSpokeGetResult
{
    public required ContentBlogSpoke Spoke { get; init; }
    public Guid? ClusterDocumentId { get; init; }
}
