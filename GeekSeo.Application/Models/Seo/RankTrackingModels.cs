namespace GeekSeo.Application.Models.Seo;

public sealed record RankSnapshot
{
    public required string Keyword { get; init; }
    public int? Position { get; init; }
    public string? PageUrl { get; init; }
    public DateOnly Date { get; init; }
}

public sealed record TrackedKeywordRequest
{
    public required string Keyword { get; init; }
    public string Location { get; init; } = "US";
    public string Device { get; init; } = "desktop";
}

public sealed record RankHistoryPoint
{
    public DateOnly Date { get; init; }
    public int? Position { get; init; }
    public string? PageUrl { get; init; }
}
