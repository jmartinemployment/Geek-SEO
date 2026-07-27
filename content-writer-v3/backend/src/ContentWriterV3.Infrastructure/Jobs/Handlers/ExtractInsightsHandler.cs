using ContentWriterV3.Domain.Entities;
using ContentWriterV3.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace ContentWriterV3.Infrastructure.Jobs.Handlers;

public class ExtractInsightsHandler : JobHandler<ExtractInsightsPayload>
{
    private readonly ContentWriterV3DbContext _dbContext;
    private readonly ILogger<ExtractInsightsHandler> _logger;
    private readonly IInsightExtractor _insightExtractor;

    public override string JobType => "ExtractInsights";

    public ExtractInsightsHandler(
        ContentWriterV3DbContext dbContext,
        ILogger<ExtractInsightsHandler> logger,
        IInsightExtractor insightExtractor)
    {
        _dbContext = dbContext;
        _logger = logger;
        _insightExtractor = insightExtractor;
    }

    protected override async Task ExecuteAsync(Job job, ExtractInsightsPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extracting insights from research run {ResearchRunId}", payload.ResearchRunId);

        var researchRun = await _dbContext.ResearchRuns.FindAsync(new object[] { payload.ResearchRunId }, cancellationToken: cancellationToken);
        if (researchRun == null)
        {
            throw new InvalidOperationException($"Research run {payload.ResearchRunId} not found");
        }

        var evidence = _dbContext.ResearchEvidence
            .Where(e => e.ResearchSourceId != Guid.Empty)
            .ToList();

        // Extract insights using LLM-like logic
        var insights = await _insightExtractor.ExtractAsync(payload.Keyword, evidence, cancellationToken);

        // Filter out lame insights
        var includedInsights = insights.Where(i => i.IncludeInOutline).ToList();

        if (includedInsights.Count == 0)
        {
            _logger.LogWarning("No insights passed filtering for research run {ResearchRunId}", payload.ResearchRunId);
            throw new InvalidOperationException("No insights survived filtering (all deemed too lame)");
        }

        // Score and rank insights
        foreach (var insight in includedInsights)
        {
            insight.CalculateRankScore();
        }

        var ranked = includedInsights.OrderByDescending(i => i.RankScore).ToList();

        for (int i = 0; i < ranked.Count; i++)
        {
            ranked[i].OrderIndex = i + 1;
        }

        _dbContext.ResearchInsights.AddRange(ranked);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Extracted {InsightCount} insights from research run {ResearchRunId}",
            ranked.Count, payload.ResearchRunId);
    }
}

public interface IInsightExtractor
{
    Task<List<ResearchInsight>> ExtractAsync(string keyword, List<ResearchEvidence> evidence, CancellationToken cancellationToken);
}

public class InsightExtractor : IInsightExtractor
{
    private readonly ILogger<InsightExtractor> _logger;

    public InsightExtractor(ILogger<InsightExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<List<ResearchInsight>> ExtractAsync(string keyword, List<ResearchEvidence> evidence, CancellationToken cancellationToken)
    {
        // TODO: Call LLM with prompt:
        // "Given research on '{keyword}' with these sources: [evidence], what are the 3-4 genuinely important insights?
        // For each insight:
        // - Title (one powerful statement)
        // - Description (what people need to understand)
        // - Why it matters (business/personal impact)
        // - What people get wrong (common misconception)
        // - Difficulty (1-10, how intellectually challenging)
        // - Importance (1-10, how critical to the decision)
        // - Skip this insight? (yes if too obvious or lame)"

        // For now, return mock insights
        var insights = new List<ResearchInsight>
        {
            new ResearchInsight(Guid.Empty, "Emergency Response Window is Hours, Not Days",
                "Once a pipe bursts, you have 2-4 hours before water damage becomes catastrophic. Most homeowners don't know this.")
            {
                WhyItMatters = "This urgency justifies the premium price of emergency service.",
                WhatPeopleGetWrong = "They think they can DIY a temporary fix and call a plumber during business hours.",
                Difficulty = 7,
                Importance = 9,
                IncludeInOutline = true
            },
            new ResearchInsight(Guid.Empty, "Preventive Maintenance ROI is 10:1",
                "One $500 inspection prevents $5000+ in water damage. The math is brutal.")
            {
                WhyItMatters = "Shifts conversation from cost to investment.",
                WhatPeopleGetWrong = "They see maintenance as an expense, not insurance.",
                Difficulty = 6,
                Importance = 8,
                IncludeInOutline = true
            },
            new ResearchInsight(Guid.Empty, "Copper vs PVC vs PEX: Lifespan Determines Your Decision",
                "Not all pipes are created equal, and mixing them creates problems.")
            {
                WhyItMatters = "The right material choice prevents 20-year regret.",
                WhatPeopleGetWrong = "They think all pipes last forever.",
                Difficulty = 8,
                Importance = 7,
                IncludeInOutline = true
            },
            new ResearchInsight(Guid.Empty, "Guarantees and Warranties Are Meaningless Without Real Support",
                "A 10-year warranty is useless if the company goes out of business or ghosts you when you call.")
            {
                WhyItMatters = "Identifies what actually matters in a plumber's credibility.",
                WhatPeopleGetWrong = "They trust paper promises over track record.",
                Difficulty = 5,
                Importance = 6,
                IncludeInOutline = true
            },
            new ResearchInsight(Guid.Empty, "DIY Drain Cleaning Products Destroy Pipes",
                "Chemical drain cleaners don't just unclog—they corrode the pipes from inside.")
            {
                WhyItMatters = "Explains why professional cleaning costs less than the damage.",
                WhatPeopleGetWrong = "They think Drano is free and harmless.",
                Difficulty = 4,
                Importance = 4,
                IncludeInOutline = false,
                ReasonForSkipping = "Too niche; only relevant to small segment. Skip unless brief specifically targets drain issues."
            }
        };

        return await Task.FromResult(insights);
    }
}

public class ExtractInsightsPayload
{
    public Guid ResearchRunId { get; set; }
    public string Keyword { get; set; } = string.Empty;
}
