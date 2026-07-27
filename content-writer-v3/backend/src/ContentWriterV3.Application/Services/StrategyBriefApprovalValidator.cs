using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Application.Services;

public interface IStrategyBriefApprovalValidator
{
    ValidationResult ValidateForApproval(StrategyBrief brief);
}

public class StrategyBriefApprovalValidator : IStrategyBriefApprovalValidator
{
    public ValidationResult ValidateForApproval(StrategyBrief brief)
    {
        var errors = new List<string>();

        // Check required fields
        if (string.IsNullOrWhiteSpace(brief.AudienceProfile))
            errors.Add("Audience profile is required");

        if (string.IsNullOrWhiteSpace(brief.BuyingStage))
            errors.Add("Buying stage is required");

        if (string.IsNullOrWhiteSpace(brief.Angle))
            errors.Add("Angle is required");

        if (string.IsNullOrWhiteSpace(brief.CallToAction))
            errors.Add("Call to action is required");

        // Check pain point is set
        if (brief.PainPointId == Guid.Empty)
            errors.Add("Pain point must be selected");

        // Check at least one evidence link
        if (brief.EvidenceLinks.Count == 0)
            errors.Add("At least one evidence link is required");

        // Check profile version is set
        if (brief.ProfileVersionId == Guid.Empty)
            errors.Add("Profile version must be selected");

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
