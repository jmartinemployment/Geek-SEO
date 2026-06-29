namespace SiteAnalyzer2.Domain.Entities;

public class CompetitorPageJsonLd
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid CompetitorPageId { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public string? ParsedType { get; set; }

    public CompetitorPage Page { get; set; } = null!;
}
