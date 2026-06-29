using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

public sealed record CompetitorDomainCrawlSummary(string Domain, int PagesCrawled);

public sealed record CompetitorCrawlStats(
    int TotalPages,
    int DomainCount,
    IReadOnlyList<CompetitorDomainCrawlSummary> Domains);

public static class CompetitorCrawlStatsQuery
{
    public static async Task<CompetitorCrawlStats> LoadAsync(
        AppDbContext db,
        Guid runId,
        CancellationToken ct = default)
    {
        var rows = await db.CompetitorPages
            .AsNoTracking()
            .Where(p => p.RunId == runId)
            .GroupBy(p => p.Domain)
            .Select(g => new { Domain = g.Key, PagesCrawled = g.Count() })
            .OrderByDescending(x => x.PagesCrawled)
            .ThenBy(x => x.Domain)
            .ToListAsync(ct);

        var domains = rows
            .Select(r => new CompetitorDomainCrawlSummary(r.Domain, r.PagesCrawled))
            .ToList();

        var totalPages = domains.Sum(d => d.PagesCrawled);
        return new CompetitorCrawlStats(totalPages, domains.Count, domains);
    }
}
