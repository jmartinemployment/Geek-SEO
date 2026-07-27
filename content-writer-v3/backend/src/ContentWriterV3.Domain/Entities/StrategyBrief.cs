namespace ContentWriterV3.Domain.Entities;

public class StrategyBrief : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid PainPointId { get; set; }
    public Guid ProfileVersionId { get; set; }
    public string AudienceProfile { get; set; } = string.Empty;
    public string BuyingStage { get; set; } = string.Empty;
    public string Angle { get; set; } = string.Empty;
    public string CallToAction { get; set; } = string.Empty;
    public BriefStatus Status { get; set; } = BriefStatus.Draft;
    public List<StrategyBriefEvidenceLink> EvidenceLinks { get; set; } = new();
    public List<ApprovalEvent> ApprovalHistory { get; set; } = new();

    public StrategyBrief() { }

    public StrategyBrief(
        Guid campaignId,
        Guid painPointId,
        Guid profileVersionId,
        string audienceProfile,
        string buyingStage,
        string angle,
        string callToAction)
    {
        CampaignId = campaignId;
        PainPointId = painPointId;
        ProfileVersionId = profileVersionId;
        AudienceProfile = audienceProfile;
        BuyingStage = buyingStage;
        Angle = angle;
        CallToAction = callToAction;
    }

    public void Approve(Guid userId)
    {
        Status = BriefStatus.Approved;
        ApprovalHistory.Add(new ApprovalEvent
        {
            StrategyBriefId = Id,
            Action = ApprovalAction.Approved,
            UserId = userId
        });
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReturnToResearch(Guid userId, string notes)
    {
        Status = BriefStatus.ReturnedToResearch;
        ApprovalHistory.Add(new ApprovalEvent
        {
            StrategyBriefId = Id,
            Action = ApprovalAction.ReturnedToResearch,
            UserId = userId,
            Notes = notes
        });
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum BriefStatus
{
    Draft,
    Approved,
    ReturnedToResearch,
    Rejected
}

public class StrategyBriefEvidenceLink : BaseEntity
{
    public Guid BriefId { get; set; }
    public Guid ResearchEvidenceId { get; set; }

    public StrategyBriefEvidenceLink() { }

    public StrategyBriefEvidenceLink(Guid briefId, Guid evidenceId)
    {
        BriefId = briefId;
        ResearchEvidenceId = evidenceId;
    }
}

public class ApprovalEvent : BaseEntity
{
    public Guid StrategyBriefId { get; set; }
    public ApprovalAction Action { get; set; }
    public Guid UserId { get; set; }
    public string? Notes { get; set; }

    public ApprovalEvent() { }

    public ApprovalEvent(Guid strategyBriefId, ApprovalAction action, Guid userId, string? notes = null)
    {
        StrategyBriefId = strategyBriefId;
        Action = action;
        UserId = userId;
        Notes = notes;
    }
}

public enum ApprovalAction
{
    Approved,
    ReturnedToResearch,
    Rejected
}
