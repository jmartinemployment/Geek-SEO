namespace SiteAnalyzer2.Domain.Entities;

public class CompetitorPageMetaTag
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CompetitorPageId { get; set; }
    public string NameOrProperty { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public CompetitorPage Page { get; set; } = null!;
}
