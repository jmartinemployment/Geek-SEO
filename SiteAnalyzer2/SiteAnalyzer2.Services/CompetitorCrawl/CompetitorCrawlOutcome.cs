namespace SiteAnalyzer2.Services.CompetitorCrawl;

public record CompetitorDomainOutcome(
    string Domain,
    string? SeedUrl,
    int SeedRankAbsolute,
    int PagesCrawled,
    bool QualityGatePassed,
    string? SkipReason);

public record CompetitorCrawlOutcome(
    int TotalPages,
    int DomainCount,
    bool FloorGatePassed,
    IReadOnlyList<CompetitorDomainOutcome> Domains,
    IReadOnlyList<string> QualityWarnings);
