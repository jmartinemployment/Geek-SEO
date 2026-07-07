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
    public Guid ProjectId { get; init; }
    public string Title { get; init; } = "Untitled Document";
    public string TargetKeyword { get; init; } = string.Empty;
    public string TargetLocation { get; init; } = "United States";
    public Guid? AnalysisRunId { get; init; }
    /// <summary>Legacy site profile id frozen at document create (optional).</summary>
    public Guid? SiteProfileId { get; init; }
    /// <summary>Set from analysis run export when <see cref="AnalysisRunId"/> is provided.</summary>
    public string SerpKeyword { get; init; } = string.Empty;
    public string? SiteFocusJson { get; init; }
    public DateTimeOffset? SiteFocusCapturedAt { get; init; }
    /// <summary>Frozen keyword bundle JSON at create/attach.</summary>
    public string? KeywordBundleJson { get; init; }
    public DateTimeOffset? KeywordBundleCapturedAt { get; init; }
    /// <summary>Pillar document when creating a spoke child (Slice 1+).</summary>
    public Guid? ParentDocumentId { get; init; }
    /// <summary><see cref="ContentDocumentKinds"/> — defaults to standalone or spoke when parent is set.</summary>
    public string? DocumentKind { get; init; }
    /// <summary>Kebab-case publish path segment; unique per project when set.</summary>
    public string? PublishSlug { get; init; }
    /// <summary><see cref="SpokeSourceTypes"/> for spoke provenance.</summary>
    public string? SpokeSourceType { get; init; }
    public string? SpokeSourcePhrase { get; init; }
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
