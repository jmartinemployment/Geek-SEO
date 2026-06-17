namespace GeekSeo.Application.Models.Seo;

public sealed record UrlAnalyzerResearchRequest
{
    /// <summary>When set, the page is crawled to derive the search keyword.</summary>
    public string? Url { get; init; }
    /// <summary>When set, SERP research runs for this keyword (no source-page crawl).</summary>
    public string? Keyword { get; init; }
    public string Location { get; init; } = "United States";
    public string Language { get; init; } = "en";
}

public sealed record SerpResearchPack
{
    public required SerpResearchPackMeta Meta { get; init; }
    public required SerpResearchIntent Intent { get; init; }
    public required SerpResearchPaf Paf { get; init; }
    public required IReadOnlyList<SerpResearchPaaItem> Paa { get; init; }
    public required IReadOnlyList<string> Pasf { get; init; }
    public required IReadOnlyList<string> SerpFeatures { get; init; }
    public required IReadOnlyList<SerpResearchOrganicItem> Organic { get; init; }
    public required IReadOnlyList<SerpResearchCompetitorOutline> CompetitorOutlines { get; init; }
    public required IReadOnlyList<SerpResearchHeading> SourceHeadings { get; init; }
    public required SerpResearchBenchmarks Benchmarks { get; init; }
    public required IReadOnlyList<string> RecommendedTerms { get; init; }
    public required IReadOnlyList<SerpResearchClosingFaqItem> ClosingFaqQuestions { get; init; }
    public required SerpResearchDirectAnswerBlock DirectAnswerBlock { get; init; }
    public required IReadOnlyList<SerpResearchMethodologyHint> MethodologyHints { get; init; }
}

public sealed record SerpResearchPackMeta
{
    public required string SourceUrl { get; init; }
    /// <summary>Search query derived from the source page (title, H1, or URL slug).</summary>
    public required string Keyword { get; init; }
    public required string Location { get; init; }
    public string Language { get; init; } = "en";
    public required string ResearchedAt { get; init; }
    public string SearchEngine { get; init; } = "Google";
    public string Device { get; init; } = "desktop";
    /// <summary>live | partial | unavailable</summary>
    public required string DataQuality { get; init; }
    /// <summary>2–4 sentences derived from the source page; intent filtering only.</summary>
    public string BusinessContext { get; init; } = "";
    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed record SerpResearchIntent
{
    public required string Primary { get; init; }
    public required string Justification { get; init; }
}

public sealed record SerpResearchPaf
{
    public required string Type { get; init; }
    public required string Format { get; init; }
    public string Text { get; init; } = "";
    public string SourceUrl { get; init; } = "";
    public string BeatStrategy { get; init; } = "";
}

public sealed record SerpResearchPaaItem
{
    public required string Question { get; init; }
    public string SerpAnswerPreview { get; init; } = "";
    public int Depth { get; init; } = 1;
}

public sealed record SerpResearchOrganicItem
{
    public required int Position { get; init; }
    public required string Url { get; init; }
    public required string Domain { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public required string ContentType { get; init; }
}

public sealed record SerpResearchCompetitorOutline
{
    public required string Url { get; init; }
    public required int Position { get; init; }
    public string H1 { get; init; } = "";
    public required IReadOnlyList<SerpResearchHeading> Headings { get; init; }
    public int EstimatedWordCount { get; init; }
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];
}

public sealed record SerpResearchHeading
{
    public required int Level { get; init; }
    public required string Text { get; init; }
}

public sealed record SerpResearchBenchmarks
{
    public int MedianWordCountTop5 { get; init; }
    public int MedianTitleLengthTop10 { get; init; }
    public required string DominantContentFormat { get; init; }
}

public sealed record SerpResearchClosingFaqItem
{
    public required string Question { get; init; }
    /// <summary>paa | pasf | suggested</summary>
    public required string Source { get; init; }
}

public sealed record SerpResearchDirectAnswerBlock
{
    public required string Instruction { get; init; }
    public bool MustBeatPaf { get; init; }
}

public sealed record SerpResearchMethodologyHint
{
    public required int Movement { get; init; }
    public required string Label { get; init; }
    public string SuggestedH2 { get; init; } = "";
    public IReadOnlyList<string> SubtopicsFromSerp { get; init; } = [];
}
