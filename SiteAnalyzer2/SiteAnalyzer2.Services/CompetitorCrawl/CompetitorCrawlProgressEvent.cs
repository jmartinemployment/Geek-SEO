namespace SiteAnalyzer2.Services.CompetitorCrawl;

public sealed record CompetitorCrawlProgressEvent(
    Guid RunId,
    string CrawlStatus,
    bool CompetitorSaved,
    int TotalPages,
    int DomainCount,
    string? Message,
    IReadOnlyList<string> QualityWarnings);
