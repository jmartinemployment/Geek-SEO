namespace GeekSeo.Application.Models.Seo;

public sealed record PageContent
{
    public required string Url { get; init; }
    public required string FullText { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public int WordCount { get; init; }
    public int HttpStatusCode { get; init; }
    public IReadOnlyList<PageHeading> Headings { get; init; } = [];
    public bool HasStructuredData { get; init; }
    public IReadOnlyList<string> StructuredDataTypes { get; init; } = [];
    public required DateTimeOffset CrawledAt { get; init; }
}

public sealed record PageHeading
{
    public required int Level { get; init; }
    public required string Text { get; init; }
}

public sealed record PageSectionLink
{
    public required string Text { get; init; }
    public required string Href { get; init; }
}

/// <summary>
/// A real heading node from a crawled page: its own text, the paragraph text that appeared
/// between it and the next heading of the same-or-shallower level, and any nested headings —
/// mirrors GeekBackend content-writer-v2's ContentDocument/Section shape so crawled-page
/// structure and generated-document structure share one tree model. Built by
/// <see cref="GeekSeoBackend.Services.SiteExtraction.PageSectionTreeBuilder"/>.
/// </summary>
public sealed record PageSection
{
    public required int Level { get; init; }
    public required string HeadingText { get; init; }
    public IReadOnlyList<string> Paragraphs { get; init; } = [];
    public IReadOnlyList<PageSectionLink> Links { get; init; } = [];
    public IReadOnlyList<PageSection> Children { get; init; } = [];

    /// <summary>True when this node has real paragraph text of its own (not just children) —
    /// the content-backed candidacy signal that replaces the old synthetic confidence gate for
    /// heading-sourced topics. No word-count minimum: a single real sentence is more honest than
    /// a synthetic confidence score, so any non-empty paragraph counts. A bare heading label with
    /// no paragraphs of its own does not.</summary>
    public bool HasOwnContent => Paragraphs.Any(p => !string.IsNullOrWhiteSpace(p));
}
