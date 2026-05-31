namespace GeekSeo.Application.Models.Seo;

public sealed record SiteAuditIssue(
    string Code,
    string Severity,
    string Message,
    string? Field = null);

public sealed record CreateSiteAuditRequest(Guid ProjectId);

public sealed record SiteAuditPageDto(
    Guid Id,
    string Url,
    int Score,
    IReadOnlyList<SiteAuditIssue> Issues,
    DateTimeOffset CrawledAt);

public sealed record SiteAuditSummaryDto(
    Guid Id,
    Guid ProjectId,
    string Status,
    int PagesCrawled,
    decimal? OverallScore,
    string? ErrorMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record SiteAuditDetailDto(
    Guid Id,
    Guid ProjectId,
    string Status,
    int PagesCrawled,
    decimal? OverallScore,
    string? ErrorMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<SiteAuditPageDto> Pages);

public sealed record AppendSiteAuditPagesRequest(IReadOnlyList<SiteAuditPageInput> Pages);

public sealed record SiteAuditPageInput(
    string Url,
    int Score,
    string IssuesJson,
    DateTimeOffset CrawledAt);

public sealed record UpdateSiteAuditStatusRequest(
    string Status,
    int PagesCrawled,
    decimal? OverallScore,
    string? ErrorMessage,
    DateTimeOffset? CompletedAt);
