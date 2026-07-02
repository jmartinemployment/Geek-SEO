namespace GeekSeo.Application.Models.Seo;

public sealed record ContentScoreHubResult
{
    public ScoreUpdateMessage? ScoreUpdate { get; init; }
    public string? PendingReason { get; init; }
}

public sealed record ScoreUpdateMessage
{
    public required int Score { get; init; }
    public required string Grade { get; init; }
    public required object Components { get; init; }
    public required IReadOnlyList<ScoreSuggestion> Suggestions { get; init; }
    public IReadOnlyList<SerpFeatureGuidance> SerpFeatures { get; init; } = [];
    public IReadOnlyList<EeatAdvisory> EeatAdvisories { get; init; } = [];
    public int? GeoScore { get; init; }
    public string? GeoGrade { get; init; }
    public object? GeoComponents { get; init; }
    public string BenchmarkQuality { get; init; } = "good";
    public DateTimeOffset? ResearchedAt { get; init; }
    public string? ScoreContextNote { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record ScoreSuggestion
{
    public required string Id { get; init; }
    public required string Component { get; init; }
    public required int PointValue { get; init; }
    public required string ActionText { get; init; }
    public required string ProposedChange { get; init; }
    /// <summary>deterministic | ai | none</summary>
    public required string ApplyMode { get; init; }
}

public sealed record ApplySuggestionRequest
{
    public required string SuggestionId { get; init; }
    /// <summary>Current editor HTML when it may differ from the last saved document.</summary>
    public string? ContentHtml { get; init; }
}

public sealed record InsertResearchCitationRequest
{
    public required string Url { get; init; }
    public string? Title { get; init; }
    /// <summary>Current editor HTML when it may differ from the last saved document.</summary>
    public string? ContentHtml { get; init; }
}

public sealed record ScoreContentRequest
{
    /// <summary>When omitted, scores the last saved document HTML.</summary>
    public string? ContentHtml { get; init; }
    public string? TargetKeyword { get; init; }
}

public sealed record ApplySuggestionResult
{
    public required string ContentHtml { get; init; }
    public required string AppliedChange { get; init; }
    public ScoreUpdateMessage? ScoreUpdate { get; init; }
}

/// <summary>completed = inline apply finished; queued = background job accepted.</summary>
public sealed record ApplySuggestionResponse
{
    public required string Outcome { get; init; }
    public ApplySuggestionResult? Result { get; init; }
    public BackgroundJobStatus? Job { get; init; }
}

public sealed record DiscoveredSource
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public string? AnchorText { get; init; }
}

public sealed record SerpFeatureGuidance
{
    public required string Feature { get; init; }
    public required string ActionText { get; init; }
    /// <summary>When set, maps to apply-suggestion id (e.g. serp_featured_snippet).</summary>
    public string? SuggestionId { get; init; }
    /// <summary>deterministic | ai | none</summary>
    public string ApplyMode { get; init; } = "none";
}

public sealed record EeatAdvisory
{
    public required string Code { get; init; }
    public required string ActionText { get; init; }
    /// <summary>Maps to apply-suggestion id (may alias geo_* suggestions).</summary>
    public string? SuggestionId { get; init; }
    /// <summary>deterministic | ai | none</summary>
    public string ApplyMode { get; init; } = "none";
    public string? ProposedChange { get; init; }
    public string? ButtonLabel { get; init; }
}

public sealed record AutoOptimizeResult
{
    public required string ContentHtml { get; init; }
    public required int PreviousScore { get; init; }
    public required int EstimatedScore { get; init; }
    public required IReadOnlyList<string> ChangesApplied { get; init; }
    public ScoreUpdateMessage? ScoreUpdate { get; init; }
}
