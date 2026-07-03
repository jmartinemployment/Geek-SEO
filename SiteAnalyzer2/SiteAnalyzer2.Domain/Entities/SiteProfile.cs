namespace SiteAnalyzer2.Domain.Entities;

public class SiteProfile
{
    public Guid Id { get; set; }
    public string SiteUrl { get; set; } = string.Empty;
    public Guid? GeekSeoProjectId { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? BusinessType { get; set; }
    public string? BusinessDescription { get; set; }
    public string? GeneratedSchemaJson { get; set; }
    /// <summary>Best business JSON-LD block from the crawled homepage (verbatim), used for recommended Block 1.</summary>
    public string? HomepageBusinessSchemaJson { get; set; }
    public DateTime? BusinessProfileAt { get; set; }
    public DateTime? LastRunAt { get; set; }

    // Site-level understanding fields
    public string? PrimaryNiche { get; set; }
    public string? NicheDescription { get; set; }
    public List<string> NicheTags { get; set; } = [];
    public string? BusinessSummary { get; set; }
    public List<string> GeoAnchorNodes { get; set; } = [];
    public string? ServiceAreaDescription { get; set; }
    public List<string> CompetitorDomains { get; set; } = [];
    public List<string> AuthorityPageUrls { get; set; } = [];
    public List<string> WritingRecommendations { get; set; } = [];

    public Project? GeekSeoProject { get; set; }
}
