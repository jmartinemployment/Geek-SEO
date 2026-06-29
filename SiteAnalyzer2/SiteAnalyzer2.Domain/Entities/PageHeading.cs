namespace SiteAnalyzer2.Domain.Entities;

public class PageHeading
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PageId { get; set; }
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Sequence { get; set; }

    public Page Page { get; set; } = null!;
}
