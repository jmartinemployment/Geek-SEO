namespace ContentWriterV3.Domain.Entities;

public class ResearchInsight : BaseEntity
{
    public Guid ResearchRunId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string WhatPeopleGetWrong { get; set; } = string.Empty;
    public int Difficulty { get; set; } = 5; // 1-10 scale, 10 = hardest
    public int Importance { get; set; } = 5; // 1-10 scale, 10 = most important
    public decimal RankScore { get; set; } // Weighted score: (Importance * 0.6) + (Difficulty * 0.4)
    public bool IncludeInOutline { get; set; } = true;
    public string? ReasonForSkipping { get; set; }
    public int OrderIndex { get; set; } // Position in outline (1 = first/hardest)
    public List<InsightEvidenceLink> EvidenceLinks { get; set; } = new();

    public ResearchInsight() { }

    public ResearchInsight(Guid researchRunId, string title, string description)
    {
        ResearchRunId = researchRunId;
        Title = title;
        Description = description;
    }

    public void CalculateRankScore()
    {
        RankScore = (Importance * 0.6m) + (Difficulty * 0.4m);
    }

    public void MarkAsSkipped(string reason)
    {
        IncludeInOutline = false;
        ReasonForSkipping = reason;
    }
}

public class InsightEvidenceLink : BaseEntity
{
    public Guid InsightId { get; set; }
    public Guid ResearchEvidenceId { get; set; }
    public int RelevanceScore { get; set; } = 100; // 0-100, how directly this evidence supports the insight

    public InsightEvidenceLink() { }

    public InsightEvidenceLink(Guid insightId, Guid evidenceId, int relevanceScore = 100)
    {
        InsightId = insightId;
        ResearchEvidenceId = evidenceId;
        RelevanceScore = relevanceScore;
    }
}
