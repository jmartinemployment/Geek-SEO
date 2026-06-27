namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Frozen keyword bundle from Site Analyzer 2 <c>content-writer-export</c> (bundle version 1).
/// </summary>
public sealed record ContentWriterSerpExport
{
    public const int CurrentBundleVersion = 1;

    public int BundleVersion { get; init; } = CurrentBundleVersion;
    public DateTimeOffset CapturedAt { get; init; }
    public required Guid RunId { get; init; }
    public Guid ProjectId { get; init; }
    public required string Keyword { get; init; }
    public string TargetSiteUrl { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long SerpSeResultsCount { get; init; }
    public DateTimeOffset? SerpCapturedAt { get; init; }
    public string? CompetitorCrawlStatus { get; init; }
    public DateTimeOffset? CompetitorCrawlFinishedAt { get; init; }

    public string? MatchedPillarTopic { get; init; }
    public string? MatchedPillarIntent { get; init; }
    public string? MatchedPillarAngle { get; init; }
    public IReadOnlyList<string> GapTopics { get; init; } = [];
    public string? WritingInstructions { get; init; }
    public IReadOnlyList<string> WritingRecommendations { get; init; } = [];

    public IReadOnlyList<ContentWriterSerpItem> Serp { get; init; } = [];
    public IReadOnlyList<ContentWriterHeading> SourceHeadings { get; init; } = [];
    public IReadOnlyList<ContentWriterCompetitorExport> Competitors { get; init; } = [];
    public ContentWriterExportBenchmarks Benchmarks { get; init; } = new();
    public IReadOnlyList<ContentWriterCitationCandidate> CitationCandidates { get; init; } = [];
}

public sealed record ContentWriterCitationCandidate
{
    public required string Url { get; init; }
    public string? Title { get; init; }
    public string? Domain { get; init; }
    public string Source { get; init; } = "organic";
}

public sealed record ContentWriterHeading
{
    public int Level { get; init; }
    public required string Text { get; init; }
    public int Sequence { get; init; }
}

public sealed record ContentWriterCompetitorExport
{
    public required string Domain { get; init; }
    public required string Url { get; init; }
    public int SeedRankAbsolute { get; init; }
    public int PagesCrawledOnDomain { get; init; }
    public IReadOnlyList<ContentWriterHeading> Headings { get; init; } = [];
    public int WordCountEstimate { get; init; }
    public string WordCountSource { get; init; } = "headings";
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];
    public bool HasFaqSchema { get; init; }
}

public sealed record ContentWriterExportBenchmarks
{
    public int MedianH2CountTop5 { get; init; }
    public int MedianWordCountTop5 { get; init; }
    public int CompetitorDomainCount { get; init; }
    public int CompetitorPageCount { get; init; }
}

public sealed record ContentWriterSerpItem
{
    public int Position { get; init; }
    public required string Type { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Domain { get; init; }
    public string? Snippet { get; init; }
    public string? Date { get; init; }
    public string? SiteName { get; init; }
    public IReadOnlyList<string> RelatedQuestions { get; init; } = [];
}
