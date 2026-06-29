namespace SiteAnalyzer2.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxCrawlDepth { get; set; } = 4;
    public int MaxCrawlPages { get; set; } = 150;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AnalysisRun> Runs { get; set; } = [];
    public ICollection<ProjectOwnedDomain> OwnedDomains { get; set; } = [];
    public ICollection<CompetitorSeedDomain> CompetitorSeeds { get; set; } = [];
}
