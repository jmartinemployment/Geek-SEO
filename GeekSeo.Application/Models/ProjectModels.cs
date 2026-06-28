namespace GeekSeo.Application.Models.Seo;

public sealed record CreateProjectRequest
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string DefaultLocation { get; init; }
    public string DefaultLanguage { get; init; } = "en";
    public string? BusinessAddress { get; init; }
    public int ServiceRadiusMiles { get; init; } = LocalServiceAreaDefaults.DefaultRadiusMiles;
    public bool LocalSeoEnabled { get; init; } = true;
}

public sealed record UpdateProjectRequest
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public string? DefaultLocation { get; init; }
    public string? DefaultLanguage { get; init; }
    public string? BusinessAddress { get; init; }
    public int? ServiceRadiusMiles { get; init; }
    public bool? LocalSeoEnabled { get; init; }
}

public sealed record CreateContentDocumentRequest
{
    /// <summary>Server-resolved on SA2 handoff — do not send from Site Analyzer.</summary>
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = "Untitled Document";
    public string TargetKeyword { get; init; } = string.Empty;
    public string TargetLocation { get; init; } = "United States";
    public Guid? AnalysisRunId { get; init; }
    /// <summary>Site Analyzer 2 <c>sa2.site_profiles.Id</c> for frozen site focus.</summary>
    public Guid? SiteProfileId { get; init; }
    /// <summary>Set from analysis run export when <see cref="AnalysisRunId"/> is provided.</summary>
    public string SerpKeyword { get; init; } = string.Empty;
    public string? SiteFocusJson { get; init; }
    public DateTimeOffset? SiteFocusCapturedAt { get; init; }
    /// <summary>Frozen SA2 <c>content-writer-export</c> JSON at create/attach.</summary>
    public string? KeywordBundleJson { get; init; }
    public DateTimeOffset? KeywordBundleCapturedAt { get; init; }
}

public sealed record UpdateContentRequest
{
    public required string ContentHtml { get; init; }
    public string? Title { get; init; }
    public string? TargetKeyword { get; init; }
    public string? TargetLocation { get; init; }
}

public sealed record CreateBackgroundJobRequest
{
    public required Guid UserId { get; init; }
    public Guid? ProjectId { get; init; }
    public required string JobType { get; init; }
    public string PayloadJson { get; init; } = "{}";
}

public sealed record BackgroundJobStatus
{
    public required Guid JobId { get; init; }
    public required string JobType { get; init; }
    public required string Status { get; init; }
    public int ProgressPercent { get; init; }
    public Guid? ResultId { get; init; }
    public string? ErrorMessage { get; init; }
}
