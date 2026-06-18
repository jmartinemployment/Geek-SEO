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

public sealed record SerpFeatureGuidance
{
    public required string Feature { get; init; }
    public required string ActionText { get; init; }
}

public sealed record EeatAdvisory
{
    public required string Code { get; init; }
    public required string ActionText { get; init; }
}

public sealed record AutoOptimizeResult
{
    public required string ContentHtml { get; init; }
    public required int PreviousScore { get; init; }
    public required int EstimatedScore { get; init; }
    public required IReadOnlyList<string> ChangesApplied { get; init; }
    public ScoreUpdateMessage? ScoreUpdate { get; init; }
}
