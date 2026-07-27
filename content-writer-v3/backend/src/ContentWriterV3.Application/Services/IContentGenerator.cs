using ContentWriterV3.Domain.Entities;

namespace ContentWriterV3.Application.Services;

public class DraftGenerationContext
{
    public StrategyBrief StrategyBrief { get; set; } = null!;
    public List<ResearchInsight> Insights { get; set; } = new();
    public SiteAudit? SiteAudit { get; set; }
    public Guid ResearchRunId { get; set; }
}

public interface IContentGenerator
{
    Task<string> GenerateDraft(DraftGenerationContext context);
}

public class MockContentGenerator : IContentGenerator
{
    public Task<string> GenerateDraft(DraftGenerationContext context)
    {
        // Mock implementation: generate a structured draft with sections for each insight
        // Real implementation would call Claude or another LLM

        var sections = new List<string>
        {
            $"# {context.StrategyBrief.Angle}",
            "",
            "## Introduction",
            $"Audience: {context.StrategyBrief.AudienceProfile}",
            $"Buying Stage: {context.StrategyBrief.BuyingStage}",
            ""
        };

        foreach (var insight in context.Insights)
        {
            sections.Add($"## {insight.Title}");
            sections.Add("");
            sections.Add($"### Why It Matters");
            sections.Add(insight.WhyItMatters);
            sections.Add("");
            sections.Add($"### What People Get Wrong");
            sections.Add(insight.WhatPeopleGetWrong);
            sections.Add("");
            sections.Add(insight.Description);
            sections.Add("");

            // Add site context references if available
            if (context.SiteAudit?.ContentInventory.Any() == true)
            {
                var relatedContent = context.SiteAudit.ContentInventory
                    .Where(cn => cn.PrimaryKeyword.Contains(insight.Title, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (relatedContent != null)
                {
                    sections.Add($"*See also: [{relatedContent.Title}]({relatedContent.Url})*");
                    sections.Add("");
                }
            }
        }

        sections.Add("## Next Steps");
        sections.Add(context.StrategyBrief.CallToAction);

        return Task.FromResult(string.Join("\n", sections));
    }
}
