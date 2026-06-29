namespace SiteAnalyzer2.Services.Integrations;

using SiteAnalyzer2.Services.Rankings;

/// <summary>
/// Keyword bundle for Content Writer freeze. Extends the legacy SERP export with run focus,
/// competitor crawl, source headings, and benchmarks (bundle version 1).
/// </summary>
public sealed record ContentWriterSerpExportDto
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
    /// <summary>Run-level merged writing instructions (run overrides site at assembly time).</summary>
    public string? WritingInstructions { get; init; }
    public IReadOnlyList<string> WritingRecommendations { get; init; } = [];

    public IReadOnlyList<ContentWriterSerpItemDto> Serp { get; init; } = [];
    public IReadOnlyList<ContentWriterHeadingDto> SourceHeadings { get; init; } = [];
    public IReadOnlyList<ContentWriterCompetitorExportDto> Competitors { get; init; } = [];
    public ContentWriterBenchmarksDto Benchmarks { get; init; } = new();
    /// <summary>Organic SERP + site authority URLs for Writer citation/source discovery.</summary>
    public IReadOnlyList<ContentWriterCitationCandidateDto> CitationCandidates { get; init; } = [];
}

public sealed record ContentWriterCitationCandidateDto
{
    public required string Url { get; init; }
    public string? Title { get; init; }
    public string? Domain { get; init; }
    /// <summary><c>organic</c> or <c>authority</c>.</summary>
    public string Source { get; init; } = "organic";
}

public sealed record ContentWriterHeadingDto
{
    public int Level { get; init; }
    public required string Text { get; init; }
    public int Sequence { get; init; }
}

public sealed record ContentWriterCompetitorExportDto
{
    public required string Domain { get; init; }
    public required string Url { get; init; }
    public int SeedRankAbsolute { get; init; }
    public int PagesCrawledOnDomain { get; init; }
    public IReadOnlyList<ContentWriterHeadingDto> Headings { get; init; } = [];
    /// <summary>Heading-text word estimate when full body text is not stored.</summary>
    public int WordCountEstimate { get; init; }
    public string WordCountSource { get; init; } = "headings";
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];
    public bool HasFaqSchema { get; init; }
}

public sealed record ContentWriterBenchmarksDto
{
    public int MedianH2CountTop5 { get; init; }
    public int MedianWordCountTop5 { get; init; }
    public int CompetitorDomainCount { get; init; }
    public int CompetitorPageCount { get; init; }
}

public sealed record ContentWriterSerpItemDto
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

public sealed record AnalysisRunSummaryDto
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Keyword { get; init; }
    public string TargetSiteUrl { get; init; } = string.Empty;
    public required string Status { get; init; }
    public long SerpSeResultsCount { get; init; }
    public int OrganicResultCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool ContentWritingReady { get; init; }
}

public sealed record SerpHtmlImportResultDto
{
    public required Guid RunId { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Keyword { get; init; }
    public string TargetSiteUrl { get; init; } = string.Empty;
    public int OrganicCount { get; init; }
    public int OrganicOnlyCount { get; init; }
    public int PaidCount { get; init; }
    public int AiOverviewCount { get; init; }
    public bool AiOverviewAvailable { get; init; }
    public int PaaCount { get; init; }
    public int CompetitorCrawlSeedCount { get; init; }
    public bool GatePassed { get; init; }
    public string GateMessage { get; init; } = string.Empty;
    public int? TargetOrganicPosition { get; init; }
    public string? TargetOrganicUrl { get; init; }
    public RankingsDeltaDto? RankingsDelta { get; init; }
}

/// <summary>
/// Site bundle for Content Writer freeze (bundle version 1). Site-level voice, niche, geo, and schema only —
/// keyword/run fields such as pillar matching and gap topics belong on the keyword bundle.
/// </summary>
public sealed record ContentWriterSiteBundleDto
{
    public const int CurrentBundleVersion = 1;

    public int BundleVersion { get; init; } = CurrentBundleVersion;
    public DateTimeOffset CapturedAt { get; init; }
    public required Guid SiteProfileId { get; init; }
    public Guid? GeekSeoProjectId { get; init; }
    public required string SiteUrl { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? BusinessProfileAt { get; init; }
    public DateTimeOffset? LastRunAt { get; init; }
    public string? BusinessType { get; init; }
    public string? BusinessDescription { get; init; }
    public string? BusinessSummary { get; init; }
    public string? GeneratedSchemaJson { get; init; }
    public string? PrimaryNiche { get; init; }
    public string? NicheDescription { get; init; }
    public IReadOnlyList<string> NicheTags { get; init; } = [];
    public IReadOnlyList<string> GeoAnchorNodes { get; init; } = [];
    public string? ServiceAreaDescription { get; init; }
    public IReadOnlyList<string> CompetitorDomains { get; init; } = [];
    public IReadOnlyList<string> AuthorityPageUrls { get; init; } = [];
    public IReadOnlyList<string> WritingRecommendations { get; init; } = [];
    public IReadOnlyList<RecommendedJsonLdSnippetDto> RecommendedHomepageJsonLd { get; init; } = [];
}
