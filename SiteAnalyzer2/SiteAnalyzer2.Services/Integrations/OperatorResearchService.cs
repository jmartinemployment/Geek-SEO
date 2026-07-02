using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Rankings;

namespace SiteAnalyzer2.Services.Integrations;

public sealed record ContentPillarDto(
    Guid RunId,
    string Keyword,
    DateTime CreatedAt,
    bool CompetitorCrawlComplete,
    bool GapTopicsReady);

public sealed record ResearchWorkflowGateDto(string Id, string Label, bool Complete);

public sealed record ResearchPackStatsDto(
    int PaaQuestionCount,
    int CompetitorPageCount,
    int CompetitorHeadingCount,
    int SourceHeadingCount,
    int GapTopicCount);

public sealed record RunResearchFocusDto(
    Guid RunId,
    string Keyword,
    string? MatchedPillarTopic,
    string? MatchedPillarIntent,
    string? MatchedPillarAngle,
    IReadOnlyList<string> GapTopics,
    string? WritingInstructions,
    bool ResearchReady,
    IReadOnlyList<ResearchWorkflowGateDto> Gates,
    ResearchPackStatsDto PackStats,
    RunRankingsSummaryDto Rankings);

/// <summary>Operator UI: content pillars per Project URL and per-run research readiness.</summary>
public sealed class OperatorResearchService(AppDbContext db, SerpRankHistoryService rankHistory)
{
    public async Task<IReadOnlyList<ContentPillarDto>> ListContentPillarsAsync(
        string siteUrl,
        CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(siteUrl);
        if (string.IsNullOrEmpty(normalized))
            return [];

        var runs = await db.AnalysisRuns.AsNoTracking()
            .Where(r => r.TargetSiteUrl == normalized && !string.IsNullOrWhiteSpace(r.Keyword))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (runs.Count == 0)
            return [];

        var runIds = runs.Select(r => r.Id).ToList();
        var competitorRunIds = await db.CompetitorPages.AsNoTracking()
            .Where(p => runIds.Contains(p.RunId))
            .Select(p => p.RunId)
            .Distinct()
            .ToListAsync(ct);
        var competitorSet = competitorRunIds.ToHashSet();

        return runs
            .Select(r => new ContentPillarDto(
                r.Id,
                r.Keyword.Trim(),
                r.CreatedAt,
                competitorSet.Contains(r.Id),
                r.GapTopics.Count > 0))
            .ToList();
    }

    public async Task<RunResearchFocusDto?> GetResearchFocusAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return null;

        var hasSerp = await db.SerpItems.AsNoTracking().AnyAsync(
            i => i.RunId == runId && i.Type == SerpItemTypes.Organic && !i.Ads,
            ct);

        var hasCompetitor = await db.CompetitorPages.AsNoTracking().AnyAsync(p => p.RunId == runId, ct);

        var targetPageIds = await db.Pages.AsNoTracking()
            .Where(p => p.RunId == runId && p.IsTargetSite)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var hasTargetHeadings = targetPageIds.Count > 0
            && await db.PageHeadings.AsNoTracking().AnyAsync(h => targetPageIds.Contains(h.PageId), ct);

        var hasComparison = await db.Findings.AsNoTracking().AnyAsync(f => f.RunId == runId, ct);

        var gapReady = run.GapTopics.Count > 0;
        var isManual = string.Equals(run.ResearchMode, ResearchModes.Manual, StringComparison.OrdinalIgnoreCase);
        IReadOnlyList<ResearchWorkflowGateDto> gates;
        bool researchReady;

        if (isManual)
        {
            (researchReady, gates) = await ManualResearchReadiness.EvaluateAsync(db, run, hasSerp, ct);
        }
        else
        {
            researchReady = hasSerp && hasCompetitor && hasTargetHeadings && hasComparison && gapReady;
            gates =
            [
                new("serp", "SERP import", hasSerp),
                new("target_crawl", "Target-site crawl", hasTargetHeadings),
                new("competitor_crawl", "Competitor crawl", hasCompetitor),
                new("comparison", "Comparison findings", hasComparison),
                new("gaps", "Gap topics assembled", gapReady),
            ];
        }

        var competitorPageIds = await db.CompetitorPages.AsNoTracking()
            .Where(p => p.RunId == runId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var competitorPageCount = competitorPageIds.Count;
        var competitorHeadingCount = competitorPageIds.Count == 0
            ? 0
            : await db.CompetitorPageHeadings.AsNoTracking()
                .CountAsync(h => competitorPageIds.Contains(h.CompetitorPageId), ct);

        var sourceHeadingCount = targetPageIds.Count == 0
            ? 0
            : await db.PageHeadings.AsNoTracking()
                .CountAsync(h => targetPageIds.Contains(h.PageId), ct);

        var paaQuestionCount = await db.SerpRelatedQueries.AsNoTracking()
            .Where(q => db.SerpItems.AsNoTracking().Any(i => i.Id == q.SerpItemId && i.RunId == runId))
            .Select(q => q.QueryText)
            .Distinct()
            .CountAsync(ct);

        var packStats = new ResearchPackStatsDto(
            paaQuestionCount,
            competitorPageCount,
            competitorHeadingCount,
            sourceHeadingCount,
            run.GapTopics.Count);

        var rankings = await rankHistory.GetSummaryAsync(runId, ct);

        return new RunResearchFocusDto(
            run.Id,
            run.Keyword.Trim(),
            run.MatchedPillarTopic,
            run.MatchedPillarIntent,
            run.MatchedPillarAngle,
            run.GapTopics,
            run.WritingInstructions,
            researchReady,
            gates,
            packStats,
            rankings);
    }
}
