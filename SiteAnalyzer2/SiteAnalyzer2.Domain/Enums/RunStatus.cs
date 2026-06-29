namespace SiteAnalyzer2.Domain.Enums;

/// <summary>
/// Operator keyword run lifecycle. Legacy pipeline advance API still uses <see cref="Failed"/> for non-SERP stage failures.
/// </summary>
public enum RunStatus
{
    /// <summary>Transient — SERP HTML import or legacy stage execution in progress.</summary>
    Running,

    /// <summary>SERP imported and SERP gate passed; competitor crawl / assembly not finished.</summary>
    SerpReady,

    /// <summary>SERP import gate failed (no usable keyword data).</summary>
    SerpFailed,

    /// <summary>Competitor crawl + research pack assembly complete (<c>gap_topics</c> persisted).</summary>
    ResearchReady,

    /// <summary>Competitor pages saved but research pack assembly did not complete.</summary>
    ResearchFailed,

    /// <summary>Legacy multi-stage pipeline stage failure (non-SERP advance path).</summary>
    Failed,

    /// <summary>Legacy — comparison stage complete via advance API. Prefer <see cref="ResearchReady"/> on operator path.</summary>
    Completed,
}
