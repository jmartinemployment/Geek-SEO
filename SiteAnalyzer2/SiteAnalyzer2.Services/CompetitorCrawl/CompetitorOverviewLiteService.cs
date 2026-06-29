using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Crawling;
using SiteAnalyzer2.Services.Filtering;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.ProfileAssembly;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.CompetitorCrawl;

/// <summary>
/// Semrush Keyword Overview / Domain Overview–style snapshot: up to five SERP competitor domains,
/// optional one ranking-page fetch per domain. Parallel to full competitor crawl — does not touch crawl job state.
/// </summary>
public sealed class CompetitorOverviewLiteService(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    RobotsTxtChecker robotsTxtChecker,
    CompetitorStructuralExtractService structuralExtractService,
    OperatorRunFocusService runFocus)
{
    public const int MaxDomains = 5;

    public async Task<CompetitorOverviewLiteDto?> GetAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.AsNoTracking()
            .Where(r => r.Id == runId)
            .Select(r => new { r.Id, r.Keyword })
            .FirstOrDefaultAsync(ct);
        if (run is null)
            return null;

        var serpItems = await LoadOrganicSerpItemsAsync(runId, run.Keyword, ct);
        if (serpItems.Count == 0)
            return null;

        var targetDomain = await db.AnalysisRuns.AsNoTracking()
            .Where(r => r.Id == runId)
            .Select(r => r.TargetSiteUrl)
            .FirstAsync(ct);
        var targetHost = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(targetDomain));

        var seeds = CompetitorCrawlService.SelectSeedsPerDomain(serpItems, targetHost, run.Keyword)
            .Take(MaxDomains)
            .ToList();

        var pages = await db.CompetitorPages.AsNoTracking()
            .Where(p => p.RunId == runId)
            .Include(p => p.Headings)
            .Include(p => p.JsonLdBlocks)
            .ToListAsync(ct);

        var pageByDomain = pages
            .GroupBy(p => p.Domain, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(p => p.DepthFromSeed).ThenBy(p => p.SeedRankAbsolute).First(),
                StringComparer.OrdinalIgnoreCase);

        var serpByDomain = serpItems
            .GroupBy(i => DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(i.Url!)).ToLowerInvariant(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(i => i.RankAbsolute).First(),
                StringComparer.OrdinalIgnoreCase);

        var rows = seeds.Select(seed =>
        {
            pageByDomain.TryGetValue(seed.Domain, out var page);
            serpByDomain.TryGetValue(seed.Domain, out var serp);
            return BuildRow(seed, serp, page);
        }).ToList();

        var analyzed = rows.Any(r => r.FetchStatus is "fetched" or "failed");
        return new CompetitorOverviewLiteDto
        {
            RunId = runId,
            Keyword = run.Keyword,
            Domains = rows,
            Analyzed = analyzed,
            CompetitorSaved = pages.Count > 0,
            Message = pages.Count > 0
                ? $"Overview: {rows.Count} SERP competitor(s); {pages.Count} page(s) in research store."
                : $"SERP snapshot: {rows.Count} competitor domain(s). Analyze to fetch ranking pages (fast).",
        };
    }

    public async Task<CompetitorOverviewLiteRunOutcome> AnalyzeAsync(Guid runId, CancellationToken ct = default)
    {
        var existingPages = await db.CompetitorPages.AsNoTracking().CountAsync(p => p.RunId == runId, ct);
        if (existingPages > 0)
        {
            var snapshot = await GetAsync(runId, ct)
                ?? throw new InvalidOperationException("Run not found or missing SERP data.");
            await runFocus.TryCompleteResearchFocusAsync(runId, ct);
            return new CompetitorOverviewLiteRunOutcome
            {
                CompetitorSaved = true,
                DomainCount = snapshot.Domains.Count,
                PagesFetched = existingPages,
                Overview = snapshot,
                Message = "Using existing competitor pages; overview refreshed from SERP + store.",
            };
        }

        var run = await db.AnalysisRuns
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var serpItems = await LoadOrganicSerpItemsAsync(runId, run.Keyword, ct);
        if (serpItems.Count == 0)
            throw new InvalidOperationException("Run has no organic SERP items. Import SERP HTML first.");

        var targetHost = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(run.TargetSiteUrl));
        var seeds = CompetitorCrawlService.SelectSeedsPerDomain(serpItems, targetHost, run.Keyword)
            .Take(MaxDomains)
            .ToList();

        var client = httpClientFactory.CreateClient(nameof(CompetitorOverviewLiteService));
        client.Timeout = TimeSpan.FromSeconds(45);

        var fetched = 0;
        foreach (var seed in seeds)
        {
            ct.ThrowIfCancellationRequested();

            if (!Uri.TryCreate(seed.Url, UriKind.Absolute, out var seedUri))
                continue;

            if (!await robotsTxtChecker.IsAllowedAsync(seedUri, ct))
                continue;

            var pageResult = await FetchSeedPageAsync(run, client, seedUri, seed, ct);
            if (pageResult is not { Page: var page })
                continue;

            if (!string.IsNullOrWhiteSpace(pageResult.Value.Html))
                structuralExtractService.ApplyStructuralExtraction(page, pageResult.Value.Html);

            db.CompetitorPages.Add(page);
            await db.SaveChangesAsync(ct);
            fetched++;
        }

        if (fetched == 0)
        {
            var serpOnly = await GetAsync(runId, ct)
                ?? throw new InvalidOperationException("Run not found.");
            return new CompetitorOverviewLiteRunOutcome
            {
                CompetitorSaved = false,
                DomainCount = serpOnly.Domains.Count,
                PagesFetched = 0,
                Overview = serpOnly,
                Message = "Could not fetch ranking pages. SERP snapshot is still available.",
            };
        }

        await runFocus.TryCompleteResearchFocusAsync(runId, ct);

        var overview = await GetAsync(runId, ct)
            ?? throw new InvalidOperationException("Run not found after analyze.");

        return new CompetitorOverviewLiteRunOutcome
        {
            CompetitorSaved = true,
            DomainCount = overview.Domains.Count,
            PagesFetched = fetched,
            Overview = overview,
            Message = $"Fetched {fetched} ranking page(s) across up to {MaxDomains} SERP competitors.",
        };
    }

    private async Task<List<SerpItem>> LoadOrganicSerpItemsAsync(Guid runId, string keyword, CancellationToken ct)
    {
        var serpItems = await db.SerpItems.AsNoTracking()
            .Where(i => i.RunId == runId
                && i.Type == SerpItemTypes.Organic
                && !i.Ads
                && i.Url != null)
            .ToListAsync(ct);

        if (serpItems.Any(i => i.FilterStatus.HasValue))
        {
            serpItems = SerpCrawlEligibility.SelectEligible(serpItems, keyword, filterApplied: true);
        }
        else
        {
            serpItems = SerpCrawlEligibility.SelectEligible(serpItems, keyword, filterApplied: false);
        }

        return serpItems;
    }

    private async Task<(CompetitorPage Page, string? Html)?> FetchSeedPageAsync(
        AnalysisRun run,
        HttpClient client,
        Uri url,
        CompetitorCrawlService.DomainSeed seed,
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
                DepthFromSeed = 0,
                SeedRankAbsolute = seed.RankAbsolute,
                CrawledAt = DateTime.UtcNow,
            };
            return (page, html);
        }
        catch (HttpRequestException)
        {
            var failedPage = new CompetitorPage
            {
                Id = Guid.NewGuid(),
                ProjectId = run.ProjectId,
                RunId = run.Id,
                Domain = seed.Domain,
                Url = CrawlPriorityMatcher.NormalizeUrl(url),
                FetchMode = FetchMode.Http,
                HttpStatus = 0,
                DepthFromSeed = 0,
                SeedRankAbsolute = seed.RankAbsolute,
                CrawledAt = DateTime.UtcNow,
            };
            return (failedPage, null);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private static CompetitorOverviewDomainRowDto BuildRow(
        CompetitorCrawlService.DomainSeed seed,
        SerpItem? serp,
        CompetitorPage? page)
    {
        var title = serp?.Title;
        var snippet = serp?.Description ?? serp?.ExtendedSnippet ?? serp?.PreSnippet;

        if (page is null)
        {
            return new CompetitorOverviewDomainRowDto
            {
                SerpRank = seed.RankAbsolute,
                Domain = seed.Domain,
                Url = seed.Url,
                Title = title,
                Snippet = snippet,
                FetchStatus = "serp_only",
            };
        }

        var h2Count = page.Headings.Count(h => h.Level == 2);
        var schemaTypes = page.JsonLdBlocks
            .Select(j => j.ParsedType)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CompetitorOverviewDomainRowDto
        {
            SerpRank = seed.RankAbsolute,
            Domain = seed.Domain,
            Url = page.Url,
            Title = title,
            Snippet = snippet,
            H2Count = h2Count,
            SchemaTypes = schemaTypes,
            HttpStatus = page.HttpStatus,
            FetchStatus = page.HttpStatus is > 0 and < 400 ? "fetched" : page.HttpStatus == 0 ? "failed" : "fetched",
        };
    }
}
