namespace SiteAnalyzer2.Domain.Enums;

/// <summary>Stable issue keys for API, UI, and fix-guide copy.</summary>
public enum AuditIssueCode
{
    BrokenPage,
    OrphanPage,
    ExcessiveCrawlDepth,
    HttpUrl,
    MissingTitleTag,
    MissingMetaDescription,
    MissingH1,
    MissingJsonLd,
}
