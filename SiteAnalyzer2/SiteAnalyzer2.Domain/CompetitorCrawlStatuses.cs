namespace SiteAnalyzer2.Domain;

/// <summary>
/// Competitor crawl job on <c>analysis_runs.competitor_crawl_status</c> — crawl + assembly, not SERP import.
/// </summary>
public static class CompetitorCrawlStatuses
{
    public const string Idle = "idle";
    public const string Running = "running";
    /// <summary>Pages persisted; gap assembly not finished or failed.</summary>
    public const string PagesSaved = "pages_saved";
    /// <summary>Pages saved and <c>gap_topics</c> assembled — research pack ready.</summary>
    public const string Complete = "complete";
    /// <summary>Crawl fetched zero pages or threw before save.</summary>
    public const string Failed = "failed";
}
