namespace SiteAnalyzer2.Domain.Entities;

/// <summary>
/// Point-in-time organic rank for the Project URL domain on a pillar run (one row per SERP import).
/// </summary>
public class SerpRankSnapshot
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid ProjectId { get; set; }

    /// <summary>1-based import count for this run (first import = 1).</summary>
    public int ImportSequence { get; set; }

    public DateTime SerpCapturedAt { get; set; }
    public DateTime RecordedAt { get; set; }

    /// <summary>Best organic position for owned domain; null when not in SERP.</summary>
    public int? TargetOrganicPosition { get; set; }
    public string? TargetOrganicUrl { get; set; }
    public int OrganicResultCount { get; set; }
}
