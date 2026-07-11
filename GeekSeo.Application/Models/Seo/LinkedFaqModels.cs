namespace GeekSeo.Application.Models.Seo;

public sealed record LinkedFaqAssignment(
    string Id,
    string Question,
    string TargetPath,
    string AnchorText,
    bool IsTargetActive);

public sealed record LinkedFaqEnrichmentRequest(
    string BusinessContext,
    string PillarKeyword,
    string CurrentHtmlExcerpt,
    IReadOnlyList<LinkedFaqAssignment> FaqAssignments);

public sealed record LinkedFaqResult(string Id, string Question, string AnswerHtml);

public sealed record LinkedFaqEnrichmentResponse(IReadOnlyList<LinkedFaqResult> FaqResults);

public sealed record GenerateLinkedFaqsResponse
{
    public required string ContentHtml { get; init; }
    public int LinkedCount { get; init; }
    public int PlainTextOnlyCount { get; init; }
    public IReadOnlyList<string> Skipped { get; init; } = [];
}
