namespace SiteAnalyzer2.Domain.Entities;

public class PageJsonLd
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PageId { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public string? ParsedType { get; set; }

    public Page Page { get; set; } = null!;
}
