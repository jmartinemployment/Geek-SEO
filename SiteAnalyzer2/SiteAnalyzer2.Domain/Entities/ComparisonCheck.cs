using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

public class ComparisonCheck
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public FindingType FindingType { get; set; }
    public ComparisonOutcome Outcome { get; set; }
    public string PayloadJson { get; set; } = "{}";

    public AnalysisRun Run { get; set; } = null!;
}
