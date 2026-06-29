using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Services.SiteAudit;

/// <summary>
/// Health score and category rollups for Site Audit overview (Semrush-style).
/// </summary>
public sealed class SiteAuditRollupService
{
    public const int ErrorWeight = 3;
    public const int WarningWeight = 1;
    public const int NoticeWeight = 0;

    public int ComputeHealthScore(int pagesCrawled, IReadOnlyList<AuditIssue> issues)
    {
        if (pagesCrawled <= 0)
            return 0;

        var penalty = issues.Sum(i => i.AffectedUrls.Count * SeverityWeight(i.Severity));
        var maxPenalty = pagesCrawled * ErrorWeight * 2;
        if (maxPenalty <= 0)
            return 100;

        var score = 100 - (int)Math.Round(100.0 * penalty / maxPenalty);
        return Math.Clamp(score, 0, 100);
    }

    public (int Healthy, int WithIssues, int Broken) ClassifyPages(
        IReadOnlyList<SiteAuditPageSnapshot> pages,
        IReadOnlyList<AuditIssue> issues)
    {
        var broken = pages.Count(p => p.HttpStatus >= 400);
        var affected = issues
            .SelectMany(i => i.AffectedUrls)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var withIssues = pages.Count(p => p.HttpStatus < 400 && affected.Contains(p.Url));
        var healthy = pages.Count - broken - withIssues;
        if (healthy < 0)
            healthy = 0;
        return (healthy, withIssues, broken);
    }

    public IReadOnlyList<SiteAuditCategoryRollup> BuildCategoryRollups(
        int pagesCrawled,
        IReadOnlyList<AuditIssue> issues)
    {
        if (pagesCrawled <= 0)
            return [];

        return Enum.GetValues<SiteAuditCategory>()
            .Select(category =>
            {
                var categoryIssues = issues.Where(i => i.Category == category).ToList();
                if (categoryIssues.Count == 0)
                {
                    return new SiteAuditCategoryRollup(category, 0, 0, 100);
                }

                var affectedPages = categoryIssues
                    .SelectMany(i => i.AffectedUrls)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                var penalty = categoryIssues.Sum(i => i.AffectedUrls.Count * SeverityWeight(i.Severity));
                var maxPenalty = pagesCrawled * ErrorWeight;
                var health = maxPenalty <= 0
                    ? 100
                    : Math.Clamp(100 - (int)Math.Round(100.0 * penalty / maxPenalty), 0, 100);

                return new SiteAuditCategoryRollup(category, categoryIssues.Count, affectedPages, health);
            })
            .Where(r => r.Category is SiteAuditCategory.Crawlability
                or SiteAuditCategory.Https
                or SiteAuditCategory.Markups
                || r.IssueCount > 0)
            .ToList();
    }

    public IReadOnlyList<AuditIssue> RankTopIssues(IReadOnlyList<AuditIssue> issues, int take = 5) =>
        issues
            .OrderByDescending(i => i.AffectedUrls.Count * SeverityWeight(i.Severity))
            .ThenByDescending(i => SeverityWeight(i.Severity))
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();

    public (int Errors, int Warnings, int Notices) CountBySeverity(IReadOnlyList<AuditIssue> issues) =>
        (
            issues.Count(i => i.Severity == AuditSeverity.Error),
            issues.Count(i => i.Severity == AuditSeverity.Warning),
            issues.Count(i => i.Severity == AuditSeverity.Notice));

    public SiteAuditIssuesPage PaginateIssues(
        IReadOnlyList<AuditIssue> issues,
        AuditSeverity? severityFilter,
        SiteAuditCategory? categoryFilter,
        SiteAuditIssueSort sort,
        int page,
        int pageSize)
    {
        var filtered = issues.AsEnumerable();
        if (severityFilter is AuditSeverity severity)
            filtered = filtered.Where(i => i.Severity == severity);
        if (categoryFilter is SiteAuditCategory category)
            filtered = filtered.Where(i => i.Category == category);

        var sorted = sort switch
        {
            SiteAuditIssueSort.PagesAsc => filtered.OrderBy(i => i.AffectedUrls.Count).ThenBy(i => i.Title),
            SiteAuditIssueSort.IssueAsc => filtered.OrderBy(i => i.Title, StringComparer.OrdinalIgnoreCase),
            SiteAuditIssueSort.UpdatedDesc => filtered.OrderByDescending(i => i.Title),
            _ => filtered.OrderByDescending(i => i.AffectedUrls.Count).ThenBy(i => i.Title),
        };

        var list = sorted.ToList();
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 100);
        var slice = list.Skip((safePage - 1) * safeSize).Take(safeSize).ToList();
        return new SiteAuditIssuesPage(slice, list.Count, safePage, safeSize);
    }

    public SiteAuditOverview BuildOverview(
        Guid auditRunId,
        Guid siteProfileId,
        string status,
        IReadOnlyList<SiteAuditPageSnapshot> pages,
        IReadOnlyList<AuditIssue> issues,
        DateTime? crawlFinishedAt,
        string? message)
    {
        var pagesCrawled = pages.Count;
        var (healthy, withIssues, broken) = ClassifyPages(pages, issues);
        var (errors, warnings, notices) = CountBySeverity(issues);
        var health = ComputeHealthScore(pagesCrawled, issues);

        return new SiteAuditOverview(
            auditRunId,
            siteProfileId,
            status,
            health,
            pagesCrawled,
            healthy,
            withIssues,
            broken,
            errors,
            warnings,
            notices,
            BuildCategoryRollups(pagesCrawled, issues),
            RankTopIssues(issues),
            crawlFinishedAt,
            message);
    }

    private static int SeverityWeight(AuditSeverity severity) =>
        severity switch
        {
            AuditSeverity.Error => ErrorWeight,
            AuditSeverity.Warning => WarningWeight,
            _ => NoticeWeight,
        };
}
