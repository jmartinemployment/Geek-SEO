namespace GeekSeo.Application.Models.Seo;

public sealed record CreateUrlResearchQueuedRequest
{
    public required Guid ProjectId { get; init; }
    public required string SourceUrl { get; init; }
    public Guid? SupersedesResearchId { get; init; }
}

public sealed record UrlResearchStatusPatch
{
    public required string Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset? ResearchedAt { get; init; }
}

public sealed record UrlResearchSummary
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string SourceUrl { get; init; }
    public required string DerivedKeyword { get; init; }
    public required string Status { get; init; }
    public string? DataQuality { get; init; }
    public DateTimeOffset? ResearchedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record UrlResearchFullWrite
{
    public required string DerivedKeyword { get; init; }
    public required string SearchLocation { get; init; }
    public string BusinessContext { get; init; } = string.Empty;
    public string GbpSource { get; init; } = "none";
    public required string Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? DataQuality { get; init; }
    public string? DataQualityNotes { get; init; }
    public required string IntentPrimary { get; init; }
    public required string IntentJustification { get; init; }
    public required string PafType { get; init; }
    public required string PafFormat { get; init; }
    public string PafText { get; init; } = string.Empty;
    public string PafSourceUrl { get; init; } = string.Empty;
    public string PafBeatStrategy { get; init; } = string.Empty;
    public required string DirectAnswerInstruction { get; init; }
    public bool MustBeatPaf { get; init; }
    public int MedianWordCountTop5 { get; init; }
    public int MedianTitleLengthTop10 { get; init; }
    public int MedianH2CountTop5 { get; init; }
    public required string DominantContentFormat { get; init; }
    public DateTimeOffset? ResearchedAt { get; init; }
    public IReadOnlyList<UrlResearchOrganicWrite> Organic { get; init; } = [];
    public IReadOnlyList<UrlResearchPaaWrite> PeopleAlsoAsk { get; init; } = [];
    public IReadOnlyList<UrlResearchPasfWrite> RelatedSearches { get; init; } = [];
    public IReadOnlyList<UrlResearchCompetitorWrite> Competitors { get; init; } = [];
    public IReadOnlyList<UrlResearchSourceHeadingWrite> SourceHeadings { get; init; } = [];
    public IReadOnlyList<UrlResearchTermWrite> RecommendedTerms { get; init; } = [];
    public IReadOnlyList<UrlResearchClosingFaqWrite> ClosingFaqs { get; init; } = [];
    public IReadOnlyList<UrlResearchSectionHintWrite> SectionHints { get; init; } = [];
}

public sealed record UrlResearchOrganicWrite
{
    public int Position { get; init; }
    public required string Url { get; init; }
    public required string Domain { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public required string ContentType { get; init; }
}

public sealed record UrlResearchPaaWrite
{
    public required string Question { get; init; }
    public string SerpAnswerPreview { get; init; } = string.Empty;
    public int Depth { get; init; } = 1;
    public int DisplayOrder { get; init; }
}

public sealed record UrlResearchPasfWrite
{
    public required string SearchText { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record UrlResearchCompetitorWrite
{
    public required string Url { get; init; }
    public int Position { get; init; }
    public string H1 { get; init; } = string.Empty;
    public int EstimatedWordCount { get; init; }
    public IReadOnlyList<UrlResearchHeadingWrite> Headings { get; init; } = [];
}

public sealed record UrlResearchHeadingWrite
{
    public int Level { get; init; }
    public required string Text { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record UrlResearchSourceHeadingWrite
{
    public int Level { get; init; }
    public required string Text { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record UrlResearchTermWrite
{
    public required string Term { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record UrlResearchClosingFaqWrite
{
    public required string Question { get; init; }
    public required string Source { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record UrlResearchSectionHintWrite
{
    public int DisplayOrder { get; init; }
    public int Movement { get; init; }
    public string Label { get; init; } = string.Empty;
    public string SuggestedH2 { get; init; } = string.Empty;
    public IReadOnlyList<string> SubtopicsFromSerp { get; init; } = [];
}

public sealed record UrlResearchQueuedJob(
    Guid UrlResearchId,
    Guid ProjectId,
    Guid UserId,
    string SourceUrl);
