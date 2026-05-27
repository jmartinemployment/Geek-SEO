namespace GeekSeoBackend.Models;

public sealed record GoogleConnectUrlResponse
{
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
};

public sealed record GoogleIntegrationStatusResponse
{
    public bool Connected { get; init; }
    public bool GscConnected { get; init; }
    public bool Ga4Connected { get; init; }
    public string? SiteUrl { get; init; }
    public string? PropertyId { get; init; }
    public DateTimeOffset? ConnectedAt { get; init; }
};

public sealed record GoogleRankingRow
{
    public string Query { get; init; } = string.Empty;
    public string Page { get; init; } = string.Empty;
    public long Impressions { get; init; }
    public long Clicks { get; init; }
    public double Ctr { get; init; }
    public double Position { get; init; }
};

public sealed record GoogleRankingsResponse
{
    public required Guid ProjectId { get; init; }
    public required string SiteUrl { get; init; }
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public IReadOnlyList<GoogleRankingRow> Rows { get; init; } = [];
};

public sealed record Ga4LandingPageRow
{
    public string LandingPage { get; init; } = string.Empty;
    public long Sessions { get; init; }
    public long Users { get; init; }
    public double Conversions { get; init; }
};

public sealed record Ga4LandingPagesResponse
{
    public required Guid ProjectId { get; init; }
    public required string PropertyId { get; init; }
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public IReadOnlyList<Ga4LandingPageRow> Rows { get; init; } = [];
};

public sealed record GoogleCallbackOutcome
{
    public required Guid ProjectId { get; init; }
    public bool GscConnected { get; init; }
    public bool Ga4Connected { get; init; }
    public string? SiteUrl { get; init; }
    public string? PropertyId { get; init; }
};
