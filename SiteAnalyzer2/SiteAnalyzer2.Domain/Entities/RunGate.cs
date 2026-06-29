using SiteAnalyzer2.Domain.Enums;

namespace SiteAnalyzer2.Domain.Entities;

public class RunGate
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RunId { get; set; }
    public PipelineStage Stage { get; set; }
    public bool Passed { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public string RowCountsJson { get; set; } = "{}";
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public AnalysisRun Run { get; set; } = null!;
}
