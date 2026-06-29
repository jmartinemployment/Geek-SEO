namespace SiteAnalyzer2.Domain.Entities;

public class PageContentBlock
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PageId { get; set; }
    public string BlockType { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int Sequence { get; set; }

    public Page Page { get; set; } = null!;
}
