namespace SiteAnalyzer2.Domain.Entities;

public class CompetitorCrawlProgressLog
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
