using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Crawling;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

public class CompetitorCrawlService(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    RobotsTxtChecker robotsTxtChecker,
    CompetitorStructuralExtractService structuralExtractService,
    CompetitorCrawlProgressPublisher progressPublisher)
{
    public async Task<CompetitorCrawlOutcome> RunAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var serpItems = await db.SerpItems
            .Where(i => i.RunId == runId
                && i.Type == SerpItemTypes.Organic
                && !i.Ads
                && i.Url != null)
            .ToListAsync(ct);

        if (serpItems.Count == 0)
            throw new InvalidOperationException("Run has no organic SERP items. Import SERP HTML first.");

        var filterHasRun = serpItems.Any(i => i.FilterStatus.HasValue);
        serpItems = SerpCrawlEligibility.SelectEligible(serpItems, run.Keyword, filterHasRun);

        if (serpItems.Count == 0)
        {
            var allOrganic = await db.SerpItems.AsNoTracking()
                .Where(i => i.RunId == runId && i.Type == SerpItemTypes.Organic && !i.Ads)
                .ToListAsync(ct);
            throw new InvalidOperationException(SerpCrawlEligibility.DescribeShortage(allOrganic, filterHasRun));
        }

        await ClearExistingCrawlAsync(runId, ct);

        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(run.TargetSiteUrl));
        var seeds = SelectSeedsPerDomain(serpItems, targetDomain, run.Keyword);
        if (seeds.Count == 0)
            throw new InvalidOperationException(
                "No competitor domains in crawl-eligible SERP rows (organic results may all be your target domain).");
        var client = httpClientFactory.CreateClient(nameof(CompetitorCrawlService));
        client.Timeout = TimeSpan.FromMinutes(2);

        var domainOutcomes = new List<CompetitorDomainOutcome>();
        var qualityWarnings = new List<string>();
        var totalPages = 0;

        foreach (var seed in seeds)
        {
            ct.ThrowIfCancellationRequested();

            if (!Uri.TryCreate(seed.Url, UriKind.Absolute, out var seedUri))
            {
                domainOutcomes.Add(new CompetitorDomainOutcome(
                    seed.Domain, seed.Url, seed.RankAbsolute, 0, true, "Invalid seed URL."));
                continue;
            }

            if (!await robotsTxtChecker.IsAllowedAsync(seedUri, ct))
            {
                domainOutcomes.Add(new CompetitorDomainOutcome(
                    seed.Domain, seed.Url, seed.RankAbsolute, 0, true, "Seed blocked by robots.txt."));
                continue;
            }

            var pathMatch = KeywordPathMatcher.Score(run.Keyword, seed.Url);
            if (pathMatch is null or "weak")
            {
                qualityWarnings.Add(
                    $"SERP ranking URL for {seed.Domain} is a weak match for pillar keyword \"{run.Keyword}\": {seed.Url}");
            }

            var pages = await CrawlSeedPageAsync(run, client, seed, ct);
            totalPages += pages.Count;

            var qualityPassed = pages.Count > 0;
            if (!qualityPassed)
            {
                qualityWarnings.Add($"Domain {seed.Domain}: seed fetch failed.");
            }

            domainOutcomes.Add(new CompetitorDomainOutcome(
                seed.Domain,
                seed.Url,
                seed.RankAbsolute,
                pages.Count,
                qualityPassed,
                pages.Count == 0 ? "Seed fetch failed or returned no HTML." : null));
            await db.SaveChangesAsync(ct);
            await PublishProgressAsync(runId, ct);
        }

        var floorPassed = totalPages >= 1;
        if (!floorPassed)
            throw new InvalidOperationException("Competitor crawl floor gate failed: zero ranking pages fetched.");

        await db.SaveChangesAsync(ct);

        var stats = await CompetitorCrawlStatsQuery.LoadAsync(db, runId, ct);

        return new CompetitorCrawlOutcome(
            stats.TotalPages,
            stats.DomainCount,
            floorPassed,
            domainOutcomes,
            qualityWarnings);
    }

    private async Task ClearExistingCrawlAsync(Guid runId, CancellationToken ct)
    {
        var existingPageIds = await db.CompetitorPages
            .Where(p => p.RunId == runId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (existingPageIds.Count == 0)
            return;

        await db.CompetitorPageHeadings
            .Where(h => existingPageIds.Contains(h.CompetitorPageId))
            .ExecuteDeleteAsync(ct);
        await db.CompetitorPageMetaTags
            .Where(m => existingPageIds.Contains(m.CompetitorPageId))
            .ExecuteDeleteAsync(ct);
        await db.CompetitorPageJsonLdBlocks
            .Where(j => existingPageIds.Contains(j.CompetitorPageId))
            .ExecuteDeleteAsync(ct);
        await db.CompetitorPages
            .Where(p => p.RunId == runId)
            .ExecuteDeleteAsync(ct);
    }

    public static List<DomainSeed> SelectSeedsPerDomain(
        IEnumerable<SerpItem> items,
        string targetDomain,
        string? pillarKeyword = null)
    {
        return items
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .Select(i => new
            {
                Item = i,
                Domain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(i.Url!)).ToLowerInvariant()
            })
            .Where(x => x.Domain.Length > 0
                && !DomainHelper.HostsMatch(x.Domain, targetDomain))
            .GroupBy(x => x.Domain, StringComparer.OrdinalIgnoreCase)
            .Select(g => g
                .OrderByDescending(x => PathMatchRank(KeywordPathMatcher.Score(pillarKeyword ?? "", x.Item.Url)))
                .ThenBy(x => x.Item.RankAbsolute)
                .First())
            .Select(x => new DomainSeed(x.Domain, x.Item.Url!, x.Item.RankAbsolute))
            .ToList();
    }

    private static int PathMatchRank(string? pathMatch) =>
        pathMatch switch
        {
            "exact" => 3,
            "strong" => 2,
            "weak" => 1,
            _ => 0,
        };

    /// <summary>Fetch the SERP ranking URL only — no whole-site BFS.</summary>
    private async Task<List<CompetitorPage>> CrawlSeedPageAsync(
        AnalysisRun run,
        HttpClient client,
        DomainSeed seed,
        CancellationToken ct)
    {
        if (!Uri.TryCreate(seed.Url, UriKind.Absolute, out var seedUri))
            return [];

        var fetched = await FetchPageAsync(run, client, seedUri, seed, depth: 0, ct);
        if (fetched is not { Page: var page })
            return [];

        db.CompetitorPages.Add(page);
        if (!string.IsNullOrWhiteSpace(fetched.Value.Html))
            structuralExtractService.ApplyStructuralExtraction(page, fetched.Value.Html);

        return [page];
    }

    private async Task<(CompetitorPage Page, string? Html)?> FetchPageAsync(
        AnalysisRun run,
        HttpClient client,
        Uri url,
        DomainSeed seed,
        int depth,
        CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            var html = response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(ct)
                : null;

            var page = new CompetitorPage
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                Domain = seed.Domain,
                Url = CrawlPriorityMatcher.NormalizeUrl(url),
                FetchMode = FetchMode.Http,
                HttpStatus = (int)response.StatusCode,
                DepthFromSeed = depth,
                SeedRankAbsolute = seed.RankAbsolute,
                CrawledAt = DateTime.UtcNow
            };
            return (page, html);
        }
        catch (HttpRequestException)
        {
            if (depth > 0)
                return null;

            var failedPage = new CompetitorPage
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                Domain = seed.Domain,
                Url = CrawlPriorityMatcher.NormalizeUrl(url),
                FetchMode = FetchMode.Http,
                HttpStatus = 0,
                DepthFromSeed = depth,
                SeedRankAbsolute = seed.RankAbsolute,
                CrawledAt = DateTime.UtcNow
            };
            return (failedPage, null);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task PublishProgressAsync(Guid runId, CancellationToken ct)
    {
        var totalPages = await db.CompetitorPages.AsNoTracking().CountAsync(p => p.RunId == runId, ct);
        var domainCount = await db.CompetitorPages.AsNoTracking()
            .Where(p => p.RunId == runId)
            .Select(p => p.Domain)
            .Distinct()
            .CountAsync(ct);

        progressPublisher.Publish(new CompetitorCrawlProgressEvent(
            runId,
            CompetitorCrawlStatuses.Running,
            CompetitorSaved: false,
            totalPages,
            domainCount,
            $"Fetched {totalPages} ranking page(s) across {domainCount} domain(s) so far.",
            []));
    }

    public record DomainSeed(string Domain, string Url, int RankAbsolute);
}
