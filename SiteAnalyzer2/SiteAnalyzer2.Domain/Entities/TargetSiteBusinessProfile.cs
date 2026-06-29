namespace SiteAnalyzer2.Domain.Entities;

public class TargetSiteBusinessProfile
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public string TargetSiteUrl { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string PrimaryServicesJson { get; set; } = "[]";
    public string? ServiceArea { get; set; }
    public string Description { get; set; } = string.Empty;
    public string GeneratedSchemaJson { get; set; } = "{}";
    public bool HasExistingSchema { get; set; }
    public bool? ExistingSchemaMatches { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReusedFromRunId { get; set; }

    public AnalysisRun Run { get; set; } = null!;
}
