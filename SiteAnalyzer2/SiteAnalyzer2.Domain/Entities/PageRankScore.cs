using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

public class PageRankScore
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public Guid PageId { get; set; }
    public GraphScope GraphScope { get; set; }
    public double Score { get; set; }

    public Page Page { get; set; } = null!;
}
