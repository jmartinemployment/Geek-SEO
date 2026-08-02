namespace GeekSeoBackend.Services;

/// <summary>
/// Site-model spine for Content Creator / Site Analyzer (through coverage).
/// Finding checks (content_gap, later broken_link, etc.) run after this spine.
/// </summary>
public static class SiteAnalyzerStepCatalog
{
    public static readonly IReadOnlyList<string> ThroughCoverage =
    [
        "schema",
        "site_urls",
        "nav",
        "headings",
        "page_content",
        "site_crawl",
        "internal_links",
        "url_patterns",
        "merging",
        "coverage",
    ];

    public const string SiteCoveragePersistStage = "site_coverage";
}
