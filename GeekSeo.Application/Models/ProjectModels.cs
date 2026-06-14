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
    public required Guid ProjectId { get; init; }
    public string Title { get; init; } = "Untitled Document";
    public string TargetKeyword { get; init; } = string.Empty;
    public string TargetLocation { get; init; } = "United States";
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
