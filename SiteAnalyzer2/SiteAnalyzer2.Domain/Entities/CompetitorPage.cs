using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

public class CompetitorPage
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? CanonicalUrl { get; set; }
    public FetchMode FetchMode { get; set; } = FetchMode.Http;
    public int HttpStatus { get; set; }
    public int? DepthFromSeed { get; set; }
    public int SeedRankAbsolute { get; set; }
    public DateTime CrawledAt { get; set; } = DateTime.UtcNow;

    public AnalysisRun Run { get; set; } = null!;
    public ICollection<CompetitorPageHeading> Headings { get; set; } = [];
    public ICollection<CompetitorPageMetaTag> MetaTags { get; set; } = [];
    public ICollection<CompetitorPageJsonLd> JsonLdBlocks { get; set; } = [];
}
