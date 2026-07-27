namespace ContentWriterV3.Domain.Entities;

public class ReconciliationProposal : BaseEntity
{
    public Guid ResearchRunId { get; set; }
    public ProposalType ProposalType { get; set; }
    public Guid? PainPointId { get; set; }
    public string ProposedDataJson { get; set; } = string.Empty;
    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public ReconciliationProposal() { }

    public ReconciliationProposal(Guid researchRunId, ProposalType proposalType, string proposedDataJson)
    {
        ResearchRunId = researchRunId;
        ProposalType = proposalType;
        ProposedDataJson = proposedDataJson;
    }

    public void Approve(Guid userId)
    {
        Status = ProposalStatus.Approved;
        ReviewedByUserId = userId;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Dismiss(Guid userId)
    {
        Status = ProposalStatus.Dismissed;
        ReviewedByUserId = userId;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ProposalType
{
    NewPainPoint,
    UpdatePainPoint,
    NewEvidenceLink
}

public enum ProposalStatus
{
    Pending,
    Approved,
    Dismissed
}
