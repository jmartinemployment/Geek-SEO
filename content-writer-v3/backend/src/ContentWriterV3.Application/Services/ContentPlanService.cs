using ContentWriterV3.Domain.Entities;
using System.Text.Json;

namespace ContentWriterV3.Application.Services;

public interface IContentPlanService
{
    ContentPlan BuildPlan(StrategyBrief brief, List<ResearchInsight> insights, List<InsightEvidenceLink> insightEvidenceLinks);
}

public class ContentPlanService : IContentPlanService
{
    public ContentPlan BuildPlan(StrategyBrief brief, List<ResearchInsight> insights, List<InsightEvidenceLink> insightEvidenceLinks)
    {
        var plan = new ContentPlan
        {
            StrategyBriefId = brief.Id,
            CampaignId = brief.CampaignId,
            Sections = new()
        };

        // Add opening section that frames the angle
        plan.Sections.Add(new ContentPlanSection
        {
            OrderIndex = 0,
            SectionName = "Opening / Angle",
            Purpose = "Hook with the content angle and establish why the reader should care",
            Insight = brief.Angle,
            ClaimConstraints = new() { "establish relevance to audience", "preview the angle" },
            LinkedInsightIds = new(),
            LinkedEvidenceIds = new()
        });

        // Add sections for each included insight, ordered by importance/difficulty (hardest first)
        var includedInsights = insights
            .Where(i => i.IncludeInOutline)
            .OrderBy(i => i.OrderIndex)
            .ToList();

        int sectionIndex = 1;
        foreach (var insight in includedInsights)
        {
            // Get evidence linked to this insight
            var linkedEvidence = insightEvidenceLinks
                .Where(el => el.InsightId == insight.Id)
                .Select(el => el.ResearchEvidenceId)
                .ToList();

            plan.Sections.Add(new ContentPlanSection
            {
                OrderIndex = sectionIndex,
                SectionName = insight.Title,
                Purpose = insight.WhyItMatters,
                Insight = insight.Description,
                ClaimConstraints = new()
                {
                    $"Explain: {insight.Description}",
                    $"Address misconception: {insight.WhatPeopleGetWrong}",
                    "Ground in evidence, not opinion"
                },
                LinkedInsightIds = new() { insight.Id },
                LinkedEvidenceIds = linkedEvidence,
                Difficulty = insight.Difficulty,
                Importance = insight.Importance
            });

            sectionIndex++;
        }

        // Final section: CTA
        plan.Sections.Add(new ContentPlanSection
        {
            OrderIndex = sectionIndex,
            SectionName = "Call to Action",
            Purpose = "Close with clear next step",
            Insight = brief.CallToAction,
            ClaimConstraints = new() { $"Match buying stage: {brief.BuyingStage}", "Create urgency without desperation" },
            LinkedInsightIds = new(),
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
    public int OrderIndex { get; set; } // 0 = opening, 1+ = insights in order of importance
    public string SectionName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Insight { get; set; } = string.Empty; // The core insight for this section
    public List<string> ClaimConstraints { get; set; } = new();
    public List<Guid> LinkedInsightIds { get; set; } = new(); // Which insights this section covers
    public List<Guid> LinkedEvidenceIds { get; set; } = new(); // Evidence supporting this section
    public int Difficulty { get; set; } // How intellectually challenging (1-10)
    public int Importance { get; set; } // How critical to the decision (1-10)
}
