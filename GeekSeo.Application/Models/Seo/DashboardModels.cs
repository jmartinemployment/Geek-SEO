using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Models.Seo;

public sealed record DashboardOverviewProject
{
    public required SeoProject Project { get; init; }
    public required IReadOnlyList<SeoContentDocument> Documents { get; init; }
    public int? LatestAuditScore { get; init; }
    public string? LatestAuditAt { get; init; }
}

public sealed record DashboardOverview
{
    public required IReadOnlyList<DashboardOverviewProject> Projects { get; init; }
    public required IReadOnlyList<SeoContentDocument> RecentDocuments { get; init; }
}
