namespace SiteAnalyzer2.Domain.Entities;

public class CrossRunLink
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public Guid FromPageId { get; set; }
    public Guid ToPageId { get; set; }
    public bool IsInternalToDomain { get; set; }
    public string Href { get; set; } = string.Empty;

    public Page FromPage { get; set; } = null!;
    public Page ToPage { get; set; } = null!;
}
