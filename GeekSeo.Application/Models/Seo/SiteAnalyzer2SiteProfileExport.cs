namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Site Analyzer 2 <c>sa2.site_profiles</c> row used to build <see cref="SiteWritingFocus"/>.
/// </summary>
public sealed record SiteAnalyzer2SiteProfileExport
{
    public required Guid Id { get; init; }
    public required string SiteUrl { get; init; }
    public string? DisplayName { get; init; }
    public Guid? GeekSeoProjectId { get; init; }
    public string? PrimaryNiche { get; init; }
    public string? NicheDescription { get; init; }
    public IReadOnlyList<string> NicheTags { get; init; } = [];
    public string? BusinessSummary { get; init; }
    public IReadOnlyList<string> GeoAnchorNodes { get; init; } = [];
    public string? ServiceAreaDescription { get; init; }
    public IReadOnlyList<string> CompetitorDomains { get; init; } = [];
    public IReadOnlyList<string> AuthorityPageUrls { get; init; } = [];
    public IReadOnlyList<string> WritingRecommendations { get; init; } = [];
    public DateTimeOffset UpdatedAt { get; init; }
}
