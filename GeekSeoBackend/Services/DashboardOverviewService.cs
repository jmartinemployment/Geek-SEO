using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services;

public sealed class DashboardOverviewService(
    IProjectRepository projects,
    IContentDocumentRepository documents,
    ISiteAuditRepository audits)
{
    public async Task<DashboardOverview> GetOverviewAsync(Guid userId, CancellationToken ct = default)
    {
        var projectList = await projects.ListByUserAsync(userId, ct);
        if (!projectList.IsSuccess || projectList.Value is null)
            return new DashboardOverview { Projects = [], RecentDocuments = [] };

        var overviewProjects = new List<DashboardOverviewProject>();
        var allDocuments = new List<SeoContentDocument>();

        foreach (var project in projectList.Value)
        {
            var docsResult = await documents.GetByProjectAsync(project.Id, ct);
            var docs = docsResult.IsSuccess && docsResult.Value is not null
                ? docsResult.Value.ToList()
                : [];

            allDocuments.AddRange(docs);

            var (score, auditAt) = await GetLatestAuditMetricsAsync(project.Id, ct);
            overviewProjects.Add(new DashboardOverviewProject
            {
                Project = project,
                Documents = docs,
                LatestAuditScore = score,
                LatestAuditAt = auditAt,
            });
        }

        var recent = allDocuments
            .OrderByDescending(d => d.Title)
            .Take(8)
            .ToList();

        return new DashboardOverview
        {
            Projects = overviewProjects,
            RecentDocuments = recent,
        };
    }

    private async Task<(int? Score, string? AuditAt)> GetLatestAuditMetricsAsync(
        Guid projectId,
        CancellationToken ct)
    {
        var list = await audits.ListByProjectAsync(projectId, ct);
        if (!list.IsSuccess || list.Value is null)
            return (null, null);

        var latest = list.Value
            .Where(a => string.Equals(a.Status, "completed", StringComparison.OrdinalIgnoreCase) && a.OverallScore.HasValue)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefault();

        if (latest?.OverallScore is not { } score)
            return (null, null);

        return ((int)Math.Round(score), latest.StartedAt.ToString("O"));
    }
}
