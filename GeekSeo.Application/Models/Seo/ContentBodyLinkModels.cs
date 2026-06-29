namespace GeekSeo.Application.Models.Seo;

public enum BodyLinkPlacementStrategy
{
    ReplaceExistingText,
    AppendToParagraph,
    SectionFooter,
}

public sealed class BodyLinkInsertionInstruction
{
    public required string LinkId { get; init; }
    /// <summary>H2 id attribute or distinctive heading text to match.</summary>
    public required string TargetHeadingId { get; init; }
    public BodyLinkPlacementStrategy PlacementStrategy { get; init; } = BodyLinkPlacementStrategy.AppendToParagraph;
    public string TargetPath { get; init; } = string.Empty;
    public string AnchorText { get; init; } = string.Empty;
    /// <summary>Transition phrase for append strategy; may include {targetPath} and {anchorText} tokens.</summary>
    public string? ContextPhrase { get; init; }
    public bool IsTargetActive { get; init; }
}

public sealed record ApplyBodyLinksRequest
{
    public IReadOnlyList<BodyLinkInsertionInstruction> Instructions { get; init; } = [];
}

public sealed record ApplyBodyLinksResponse
{
    public required string ContentHtml { get; init; }
    public int AppliedCount { get; init; }
    public bool Changed { get; init; }
}
