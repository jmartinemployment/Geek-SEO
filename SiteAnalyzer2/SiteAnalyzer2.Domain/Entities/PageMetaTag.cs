namespace SiteAnalyzer2.Domain.Entities;

public class PageMetaTag
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PageId { get; set; }
    public string NameOrProperty { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public Page Page { get; set; } = null!;
}
