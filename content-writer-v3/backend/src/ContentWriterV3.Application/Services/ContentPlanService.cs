using ContentWriterV3.Domain.Entities;
using System.Text.Json;

namespace ContentWriterV3.Application.Services;

public interface IContentPlanService
{
    ContentPlan BuildPlan(StrategyBrief brief, PainPoint painPoint, List<ResearchEvidence> linkedEvidence);
}

public class ContentPlanService : IContentPlanService
{
    public ContentPlan BuildPlan(StrategyBrief brief, PainPoint painPoint, List<ResearchEvidence> linkedEvidence)
    {
        var plan = new ContentPlan
        {
            StrategyBriefId = brief.Id,
            CampaignId = brief.CampaignId,
            Sections = new()
        };

        // Section 1: Opening (addresses reader symptom)
        plan.Sections.Add(new ContentPlanSection
        {
            Purpose = "Capture reader's problem and establish relevance",
            PainToOpenWith = painPoint.ReaderSymptom,
            SectionName = "Opening / Hook",
            ClaimConstraints = new() { "must include reader symptom", "must reference pain point" },
            LinkedEvidenceIds = linkedEvidence.Where(e => e.Statement.Contains("symptom", StringComparison.OrdinalIgnoreCase)).Select(e => e.Id).ToList()
        });

        // Section 2: Cost of Inaction
        plan.Sections.Add(new ContentPlanSection
        {
            Purpose = "Establish urgency and cost of not acting",
            PainToOpenWith = painPoint.CostOfInaction,
            SectionName = "Cost of Inaction",
            ClaimConstraints = new() { "must quantify cost", "must establish urgency" },
            LinkedEvidenceIds = linkedEvidence.Where(e => e.Statement.Contains("cost", StringComparison.OrdinalIgnoreCase)).Select(e => e.Id).ToList()
        });

        // Section 3: Solution / Offer
        plan.Sections.Add(new ContentPlanSection
        {
            Purpose = "Present the solution using offer terminology",
            PainToOpenWith = painPoint.OfferTerminology,
            SectionName = "Solution",
            ClaimConstraints = new() { "use offered terminology", "connect to pain point" },
            LinkedEvidenceIds = linkedEvidence.Where(e => e.ApprovedForClaim).Select(e => e.Id).ToList()
        });

        // Section 4: Address Objections
        if (painPoint.Objections.Count > 0)
        {
            plan.Sections.Add(new ContentPlanSection
            {
                Purpose = "Address common objections",
                PainToOpenWith = $"Objections: {string.Join(", ", painPoint.Objections)}",
                SectionName = "Objections & Answers",
                ClaimConstraints = new() { "address each objection", "provide evidence-backed answers" },
                LinkedEvidenceIds = linkedEvidence.Select(e => e.Id).ToList()
            });
        }

        // Section 5: CTA / Next Steps
        plan.Sections.Add(new ContentPlanSection
        {
            Purpose = "Drive action with clear call-to-action",
            PainToOpenWith = brief.CallToAction,
            SectionName = "Call to Action",
            ClaimConstraints = new() { "match buying stage: " + brief.BuyingStage, "create urgency" },
            LinkedEvidenceIds = new()
        });

        return plan;
    }
}

public class ContentPlan
{
    public Guid StrategyBriefId { get; set; }
    public Guid CampaignId { get; set; }
    public List<ContentPlanSection> Sections { get; set; } = new();

    public string ToJson() => JsonSerializer.Serialize(this);
}

public class ContentPlanSection
{
    public string SectionName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string PainToOpenWith { get; set; } = string.Empty;
    public List<string> ClaimConstraints { get; set; } = new();
    public List<Guid> LinkedEvidenceIds { get; set; } = new();
}
