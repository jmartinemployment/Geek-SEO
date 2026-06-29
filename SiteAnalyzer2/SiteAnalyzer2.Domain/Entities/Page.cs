using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

public class Page
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? CanonicalUrl { get; set; }
    public FetchMode FetchMode { get; set; } = FetchMode.Http;
    public int HttpStatus { get; set; }
    public bool IsTargetSite { get; set; }
    public int? DepthFromHomepage { get; set; }
    public string? HtmlContent { get; set; }

    public AnalysisRun Run { get; set; } = null!;
    public ICollection<PageHeading> Headings { get; set; } = [];
    public ICollection<PageMetaTag> MetaTags { get; set; } = [];
    public ICollection<PageJsonLd> JsonLdBlocks { get; set; } = [];
    public ICollection<PageContentBlock> ContentBlocks { get; set; } = [];
    public ICollection<PageRankScore> PageRankScores { get; set; } = [];
}
