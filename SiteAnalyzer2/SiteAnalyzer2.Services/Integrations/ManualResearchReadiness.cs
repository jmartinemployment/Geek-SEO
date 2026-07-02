using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.Integrations;

internal static class ManualResearchReadiness
{
    public static IReadOnlyList<string> RequiredSupplementalLanes(string? topicSlug) =>
        string.Equals(topicSlug, "customer-journey", StringComparison.OrdinalIgnoreCase)
            ? [SerpResearchLanes.Gov, SerpResearchLanes.Wiki]
            : [];

    public static async Task<(bool Ready, IReadOnlyList<ResearchWorkflowGateDto> Gates)> EvaluateAsync(
        AppDbContext db,
        AnalysisRun run,
        bool hasKeywordSerp,
        CancellationToken ct)
    {
        var stats = await LoadLaneStatsAsync(db, run.Id, ct);
        var gates = new List<ResearchWorkflowGateDto>
        {
            new("keyword", "Keyword SERP", hasKeywordSerp),
            new("paa", "People Also Ask", stats.PaaCount > 0),
            new("edu", "Research (.edu)", stats.OrganicCount(SerpResearchLanes.Edu) > 0),
            new("gov", "Government", stats.OrganicCount(SerpResearchLanes.Gov) > 0),
            new("local", "Local", stats.LocalReady),
            new("wiki", "Wikipedia", stats.OrganicCount(SerpResearchLanes.Wiki) > 0),
        };

        if (!hasKeywordSerp)
            return (false, gates);

        var ready = RequiredSupplementalLanes(run.TopicSlug)
            .All(lane => stats.OrganicCount(lane) > 0);

        return (ready, gates);
    }

    private static async Task<LaneImportStats> LoadLaneStatsAsync(
        AppDbContext db,
        Guid runId,
        CancellationToken ct)
    {
        var items = await db.SerpItems.AsNoTracking()
            .Include(i => i.RelatedQueries)
            .Where(i => i.RunId == runId)
            .ToListAsync(ct);

        var run = await db.AnalysisRuns.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == runId, ct);

        var organicsByLane = items
            .Where(i => string.Equals(i.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase) && !i.Ads)
            .GroupBy(i => i.ResearchLane ?? SerpResearchLanes.Keyword, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var paaCount = items
            .Where(i => string.Equals(i.ResearchLane, SerpResearchLanes.Paa, StringComparison.OrdinalIgnoreCase))
            .SelectMany(i => i.RelatedQueries)
            .Count(q => !string.IsNullOrWhiteSpace(q.QueryText));

        var localOrganics = organicsByLane.GetValueOrDefault(SerpResearchLanes.Local);
        var localReady = localOrganics > 0 || run?.SerpLocalPackPresent == true;

        return new LaneImportStats(organicsByLane, paaCount, localReady);
    }

    private sealed class LaneImportStats(
        Dictionary<string, int> organicsByLane,
        int paaCount,
        bool localReady)
    {
        public int PaaCount { get; } = paaCount;
        public bool LocalReady { get; } = localReady;

        public int OrganicCount(string lane) =>
            organicsByLane.GetValueOrDefault(lane);
    }
}
