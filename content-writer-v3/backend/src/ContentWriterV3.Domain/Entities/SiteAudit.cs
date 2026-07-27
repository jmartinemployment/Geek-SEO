namespace ContentWriterV3.Domain.Entities;

public class SiteAudit : BaseEntity
{
    public Guid ClientId { get; set; }
    public string WebsiteUrl { get; set; } = string.Empty;
    public string PositioningSummary { get; set; } = string.Empty;
    public List<string> PrimaryOfferingsJson { get; set; } = new(); // Service/product names
    public List<string> AudienceSegmentsJson { get; set; } = new(); // Target personas
    public List<string> CompetitorsJson { get; set; } = new(); // Competitor names
    public string CompetitivePositioning { get; set; } = string.Empty;
    public List<ContentNode> ContentInventory { get; set; } = new();
    public List<TopicalCluster> TopicalClusters { get; set; } = new();
    public List<ContentGap> IdentifiedGaps { get; set; } = new();
    public DateTime AuditedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    public SiteAudit() { }

    public SiteAudit(Guid clientId, string websiteUrl)
    {
        ClientId = clientId;
        WebsiteUrl = websiteUrl;
        AuditedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }
}

public class ContentNode : BaseEntity
{
    public Guid SiteAuditId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string PrimaryKeyword { get; set; } = string.Empty;
    public ContentNodeType Type { get; set; } // Cornerstone, Pillar, Supporting, FAQ
    public int WordCount { get; set; }
    public int InboundLinks { get; set; }
    public List<string> OutboundLinksJson { get; set; } = new();
    public bool IsCornerstoneContent { get; set; }
    public int AuthorityScore { get; set; } // 1-10 based on inbound links, internal link depth
}

public enum ContentNodeType
{
    Cornerstone,
    Pillar,
    Supporting,
    FAQ,
    Service,
    ProductPage,
    Other
}

public class TopicalCluster : BaseEntity
{
    public Guid SiteAuditId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string CornerstonePage { get; set; } = string.Empty; // URL
    public List<string> SupportingPages { get; set; } = new(); // URLs
    public int ContentDepth { get; set; } // Number of pieces covering this topic
    public int Authority { get; set; } // 1-10 aggregate authority for this topic
}

public class ContentGap : BaseEntity
{
    public Guid SiteAuditId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GapPriority Priority { get; set; }
    public string? RelatedCompetitorContent { get; set; }
    public bool AlignedWithOfferings { get; set; }
}

public enum GapPriority
{
    Critical,    // Competitors cover it; client doesn't. Audience definitely needs it.
    High,        // Related to existing offerings; would strengthen positioning
    Medium,      // Nice to have; fills out the strategy
    Low          // Niche; interesting but not strategically important
}
