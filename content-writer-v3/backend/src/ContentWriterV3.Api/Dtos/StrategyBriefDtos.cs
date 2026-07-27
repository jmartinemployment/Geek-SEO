namespace ContentWriterV3.Api.Dtos;

public class CreateStrategyBriefRequest
{
    public Guid CampaignId { get; set; }
    public Guid PainPointId { get; set; }
    public Guid ProfileVersionId { get; set; }
    public string AudienceProfile { get; set; } = string.Empty;
    public string BuyingStage { get; set; } = string.Empty;
    public string Angle { get; set; } = string.Empty;
    public string CallToAction { get; set; } = string.Empty;
    public List<Guid> LinkedEvidenceIds { get; set; } = new();
}

public class UpdateStrategyBriefRequest
{
    public string? AudienceProfile { get; set; }
    public string? BuyingStage { get; set; }
    public string? Angle { get; set; }
    public string? CallToAction { get; set; }
}

public class ApproveBriefRequest
{
    public Guid UserId { get; set; }
}

public class ReturnToResearchRequest
{
    public Guid UserId { get; set; }
    public string? Notes { get; set; }
}

public class StrategyBriefResponse
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid PainPointId { get; set; }
    public string AudienceProfile { get; set; } = string.Empty;
    public string BuyingStage { get; set; } = string.Empty;
    public string Angle { get; set; } = string.Empty;
    public string CallToAction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int EvidenceLinkCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
