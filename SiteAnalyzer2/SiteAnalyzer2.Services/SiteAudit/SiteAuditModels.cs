using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Services.SiteAudit;

public sealed record SiteAuditPageSnapshot(
    Guid PageId,
    string Url,
    int HttpStatus,
    int? DepthFromHomepage,
    bool IsTargetSite,
    IReadOnlyList<string> HeadingLevels,
    IReadOnlyList<(string NameOrProperty, string Content)> MetaTags,
    IReadOnlyList<string> JsonLdTypes);

public sealed record SiteAuditLinkSnapshot(
    string SourceUrl,
    string TargetUrl,
    bool IsInternal);

public sealed record SiteAuditCheckInput(
    string SiteUrl,
    IReadOnlyList<SiteAuditPageSnapshot> Pages,
    IReadOnlyList<SiteAuditLinkSnapshot> InternalLinks);

public sealed record AuditIssue(
    AuditIssueCode Code,
    SiteAuditCategory Category,
    AuditSeverity Severity,
    string Title,
    string Summary,
    IReadOnlyList<string> AffectedUrls,
    string FixGuide);

public sealed record SiteAuditCategoryRollup(
    SiteAuditCategory Category,
    int IssueCount,
    int AffectedPageCount,
    int HealthPercent);

public sealed record SiteAuditOverview(
    Guid AuditRunId,
    Guid SiteProfileId,
    string Status,
    int HealthScore,
    int PagesCrawled,
    int PagesHealthy,
    int PagesWithIssues,
    int PagesBroken,
    int ErrorsCount,
    int WarningsCount,
    int NoticesCount,
    IReadOnlyList<SiteAuditCategoryRollup> Categories,
    IReadOnlyList<AuditIssue> TopIssues,
    DateTime? CrawlFinishedAt,
    string? Message);

public sealed record SiteAuditIssuesPage(
    IReadOnlyList<AuditIssue> Issues,
    int TotalCount,
    int Page,
    int PageSize);

public enum SiteAuditIssueSort
{
    PagesDesc,
    PagesAsc,
    IssueAsc,
    UpdatedDesc,
}
