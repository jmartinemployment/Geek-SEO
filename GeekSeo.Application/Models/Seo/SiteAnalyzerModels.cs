namespace GeekSeo.Application.Models.Seo;

public sealed record SiteAnalyzerStepResponse
{
    public required int StepNumber { get; init; }
    /// <summary>green | red | running | pending</summary>
    public required string Status { get; init; }
    public required string Message { get; init; }
    public string Log { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, int>? Counts { get; init; }
}

public sealed record SiteAnalyzerStateResponse
{
    public Guid? SiteResearchId { get; init; }
    public required string SiteUrl { get; init; }
    public IReadOnlyList<SiteAnalyzerStepResponse> SiteIndexSteps { get; init; } = [];
    public Guid? ActiveUrlResearchId { get; init; }
    public string? Keyword { get; init; }
    public IReadOnlyList<SiteAnalyzerStepResponse> KeywordPackSteps { get; init; } = [];
    public bool HandoffEnabled { get; init; }
    public string? BlockReason { get; init; }
}

public sealed record CreateSiteAnalyzerPackRequest
{
    public required string Keyword { get; init; }
    public string Location { get; init; } = "United States";
}

public sealed record SiteResearchPageWrite
{
    public required string Url { get; init; }
    public string Html { get; init; } = string.Empty;
    public string HeadingsJson { get; init; } = "[]";
    public string JsonLdJson { get; init; } = "[]";
    public bool ExtractSuccess { get; init; }
    public string? ExtractError { get; init; }
}

public sealed record SiteResearchStep1Write
{
    public required IReadOnlyList<string> DiscoveredUrls { get; init; }
}

public sealed record SiteResearchStep4Write
{
    public required string BusinessSummary { get; init; }
    public required string InternalLinkMapJson { get; init; }
}

public sealed record SiteAnalyzerStepRunUpsert
{
    public Guid? SiteResearchId { get; init; }
    public Guid? UrlResearchId { get; init; }
    public required int StepNumber { get; init; }
    public required string Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Log { get; init; } = string.Empty;
    public string? CountsJson { get; init; }
}

public sealed record SiteAnalyzerStepRunRow
{
    public required int StepNumber { get; init; }
    public required string Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Log { get; init; } = string.Empty;
    public string? CountsJson { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed record CreateSiteResearchRequest
{
    public required Guid ProjectId { get; init; }
    public required string SiteUrl { get; init; }
}

public sealed record CreateSiteAnalyzerPackQueuedRequest
{
    public required Guid ProjectId { get; init; }
    public required Guid SiteResearchId { get; init; }
    public required string Keyword { get; init; }
    public required string SourceUrl { get; init; }
    public string SearchLocation { get; init; } = "United States";
}
