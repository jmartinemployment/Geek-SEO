namespace GeekSeoBackend.Services;

/// <summary>
/// Site-model spine for Content Creator / Site Analyzer: raw crawl/discovery only. Gaps and
/// existing pages are no longer computed as pipeline steps here — GeekAPI reads them as raw
/// facts on demand (discovered-urls, content-gaps; see SiteContentCoverageMatcher).
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
    ];

    public const string SiteCoveragePersistStage = "site_coverage";
}
