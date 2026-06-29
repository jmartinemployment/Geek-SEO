namespace SiteAnalyzer2.Domain.Entities;

public class ProjectOwnedDomain
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Domain { get; set; } = string.Empty;

    public Project Project { get; set; } = null!;
}
