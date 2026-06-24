namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Frozen site bundle from Site Analyzer 2 <c>content-writer-bundle</c> (bundle version 1).
/// Site-level voice and niche only — keyword/run fields belong on <see cref="ContentWriterSerpExport"/>.
/// </summary>
public sealed record ContentWriterSiteBundle
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
    public IReadOnlyList<RecommendedJsonLdSnippet> RecommendedHomepageJsonLd { get; init; } = [];
}

public sealed record RecommendedJsonLdSnippet
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Json { get; init; }
    public required string ScriptTag { get; init; }
}
