using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.Integrations;

public sealed class ContentWriterExportService(
    AppDbContext db,
    OperatorResearchService researchFocus)
{
    public async Task<ContentWriterSerpExportDto?> GetExportAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null)
            return null;

        var items = await db.SerpItems
            .AsNoTracking()
            .Include(i => i.RelatedQueries)
            .Where(i => i.RunId == runId)
            .OrderBy(i => i.RankAbsolute)
            .ToListAsync(ct);

        var competitorPages = await db.CompetitorPages
            .AsNoTracking()
            .Include(p => p.Headings)
            .Include(p => p.JsonLdBlocks)
            .Where(p => p.RunId == runId)
            .OrderBy(p => p.SeedRankAbsolute)
            .ThenBy(p => p.DepthFromSeed)
            .ToListAsync(ct);

        var sourcePages = await db.Pages
            .AsNoTracking()
            .Include(p => p.Headings)
            .Where(p => p.RunId == runId && p.IsTargetSite)
            .ToListAsync(ct);

        var authorityPageUrls = await db.SiteProfiles.AsNoTracking()
            .Where(p => p.SiteUrl == run.TargetSiteUrl)
            .Select(p => p.AuthorityPageUrls)
            .FirstOrDefaultAsync(ct) ?? [];

        return ContentWriterKeywordBundleBuilder.Build(
            run,
            items,
            competitorPages,
            sourcePages,
            DateTimeOffset.UtcNow,
            authorityPageUrls);
    }

    public async Task<IReadOnlyList<AnalysisRunSummaryDto>> ListByProjectAsync(
        Guid projectId,
        CancellationToken ct = default)
    {
        var runs = await db.AnalysisRuns
            .AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (runs.Count == 0)
            return [];

        var runIds = runs.Select(r => r.Id).ToList();
        var organicCounts = await db.SerpItems
            .AsNoTracking()
            .Where(i => runIds.Contains(i.RunId) && i.Type == SerpItemTypes.Organic && !i.Ads)
            .GroupBy(i => i.RunId)
            .Select(g => new { RunId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RunId, x => x.Count, ct);

        var readiness = new Dictionary<Guid, bool>();
        foreach (var runId in runIds)
        {
            var focus = await researchFocus.GetResearchFocusAsync(runId, ct);
            readiness[runId] = focus?.ResearchReady ?? false;
        }

        return runs.Select(run =>
        {
            organicCounts.TryGetValue(run.Id, out var organicCount);
            readiness.TryGetValue(run.Id, out var researchReady);
            return new AnalysisRunSummaryDto
            {
                Id = run.Id,
                ProjectId = run.ProjectId,
                Keyword = run.Keyword,
                TargetSiteUrl = run.TargetSiteUrl,
                Status = run.Status.ToString(),
                SerpSeResultsCount = run.SerpSeResultsCount ?? 0,
                OrganicResultCount = organicCount,
                CreatedAt = new DateTimeOffset(run.CreatedAt, TimeSpan.Zero),
                ContentWritingReady = researchReady,
            };
        }).ToList();
    }

    public static ContentWriterSerpExportDto BuildExport(
        Domain.Entities.AnalysisRun run,
        IReadOnlyList<Domain.Entities.SerpItem> items) =>
        ContentWriterKeywordBundleBuilder.Build(run, items, [], [], DateTimeOffset.UtcNow);
}
