namespace SiteAnalyzer2.Domain.Entities;

public class CompetitorPageHeading
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CompetitorPageId { get; set; }
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Sequence { get; set; }

    public CompetitorPage Page { get; set; } = null!;
}
