using SiteAnalyzer2.Domain;

namespace SiteAnalyzer2.Domain.Entities;

public class SiteAuditRun
{
    public Guid Id { get; set; }
    public Guid SiteProfileId { get; set; }
    public string Status { get; set; } = SiteAuditStatuses.Running;
    public string? Message { get; set; }
    public DateTime? CrawlStartedAt { get; set; }
    public DateTime? CrawlFinishedAt { get; set; }
    public int PagesCrawled { get; set; }
    public int PagesHealthy { get; set; }
    public int PagesWithIssues { get; set; }
    public int PagesBroken { get; set; }
    public int HealthScore { get; set; }
    public int ErrorsCount { get; set; }
    public int WarningsCount { get; set; }
    public int NoticesCount { get; set; }
    public string CategoryRollupsJson { get; set; } = "{}";

    public SiteProfile SiteProfile { get; set; } = null!;
}
