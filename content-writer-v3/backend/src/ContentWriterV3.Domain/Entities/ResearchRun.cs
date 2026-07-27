namespace ContentWriterV3.Domain.Entities;

public class ResearchRun : BaseEntity
{
    public Guid CampaignId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public ResearchRunStatus Status { get; set; } = ResearchRunStatus.Queued;
    public int DiscoveredSourceCount { get; set; }
    public decimal SpentBudget { get; set; }
    public decimal MaxBudget { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ResearchSource> Sources { get; set; } = new();
    public List<ReconciliationProposal> Proposals { get; set; } = new();

    public ResearchRun() { }

    public ResearchRun(Guid campaignId, string keyword, decimal maxBudget)
    {
        CampaignId = campaignId;
        Keyword = keyword;
        MaxBudget = maxBudget;
    }

    public void MarkRunning() => Status = ResearchRunStatus.Running;
    public void MarkCompleted() => Status = ResearchRunStatus.Completed;
    public void MarkPartiallyCompleted() => Status = ResearchRunStatus.CompletedWithPartialCoverage;
    public void MarkFailed(string errorMessage) => (Status, ErrorMessage) = (ResearchRunStatus.Failed, errorMessage);
}

public enum ResearchRunStatus
{
    Queued,
    Running,
    Completed,
    CompletedWithPartialCoverage,
    Failed
}
