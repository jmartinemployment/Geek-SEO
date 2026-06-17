namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Single read DTO for research-backed writing — built from persisted <c>seo_url_research_*</c> rows.
/// </summary>
public sealed record WritingResearchContext
{
    public required Guid UrlResearchId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required string SourceUrl { get; init; }
    public required string DerivedKeyword { get; init; }
    public required string SearchLocation { get; init; }
    public string BusinessContext { get; init; } = string.Empty;
    public required string IntentPrimary { get; init; }
    public required string IntentJustification { get; init; }
    public required WritingResearchPaf Paf { get; init; }
    public required string DirectAnswerInstruction { get; init; }
    public bool MustBeatPaf { get; init; }
    public required WritingResearchBenchmarks Benchmarks { get; init; }
    public string? DataQuality { get; init; }
    public string? DataQualityNotes { get; init; }
    public DateTimeOffset? ResearchedAt { get; init; }
    public IReadOnlyList<WritingResearchOrganic> Organic { get; init; } = [];
    public IReadOnlyList<WritingResearchPaa> PeopleAlsoAsk { get; init; } = [];
    public IReadOnlyList<WritingResearchPasf> RelatedSearches { get; init; } = [];
    public IReadOnlyList<WritingResearchCompetitor> Competitors { get; init; } = [];
    public IReadOnlyList<WritingResearchHeading> SourceHeadings { get; init; } = [];
    public IReadOnlyList<WritingResearchTerm> RecommendedTerms { get; init; } = [];
    public IReadOnlyList<WritingResearchClosingFaq> ClosingFaqs { get; init; } = [];
    public IReadOnlyList<WritingResearchSectionHint> SectionHints { get; init; } = [];
}

public sealed record WritingResearchPaf
{
    public required string Type { get; init; }
    public required string Format { get; init; }
    public string Text { get; init; } = string.Empty;
    public string SourceUrl { get; init; } = string.Empty;
    public string BeatStrategy { get; init; } = string.Empty;
}

public sealed record WritingResearchBenchmarks
{
    public int MedianWordCountTop5 { get; init; }
    public int MedianTitleLengthTop10 { get; init; }
    public int MedianH2CountTop5 { get; init; }
    public required string DominantContentFormat { get; init; }
}

public sealed record WritingResearchOrganic
{
    public int Position { get; init; }
    public required string Url { get; init; }
    public required string Domain { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public required string ContentType { get; init; }
}

public sealed record WritingResearchPaa
{
    public required string Question { get; init; }
    public string SerpAnswerPreview { get; init; } = string.Empty;
    public int Depth { get; init; } = 1;
    public int DisplayOrder { get; init; }
}

public sealed record WritingResearchPasf
{
    public required string SearchText { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record WritingResearchCompetitor
{
    public required string Url { get; init; }
    public int Position { get; init; }
    public string H1 { get; init; } = string.Empty;
    public int EstimatedWordCount { get; init; }
    public IReadOnlyList<WritingResearchHeading> Headings { get; init; } = [];
}

public sealed record WritingResearchHeading
{
    public int Level { get; init; }
    public required string Text { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record WritingResearchTerm
{
    public required string Term { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record WritingResearchClosingFaq
{
    public required string Question { get; init; }
    public required string Source { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record WritingResearchSectionHint
{
    public int DisplayOrder { get; init; }
    public int Movement { get; init; }
    public string Label { get; init; } = string.Empty;
    public string SuggestedH2 { get; init; } = string.Empty;
    public IReadOnlyList<string> SubtopicsFromSerp { get; init; } = [];
}

public sealed record ResearchDraftRequest
{
    public required WritingResearchContext Research { get; init; }
    public string? Title { get; init; }
    public int TargetWordCount { get; init; }
}

public sealed record AttachUrlResearchRequest
{
    public required Guid UrlResearchId { get; init; }
}
