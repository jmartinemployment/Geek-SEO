namespace SiteAnalyzer2.Domain.Entities;

public class CrawlPriorityUrlPattern
{
    public Guid Id { get; set; }
    public string Pattern { get; set; } = string.Empty;
}
