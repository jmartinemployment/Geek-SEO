using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.CompetitorCrawl;
using SiteAnalyzer2.Services.Integrations;
using SiteAnalyzer2.Services.ProfileAssembly;

namespace SiteAnalyzer2.Api.Controllers;

[ApiController]
[Route("runs/{runId:guid}/competitor-crawl")]
public class CompetitorCrawlController(
    AppDbContext db,
    CompetitorCrawlJobService crawlJobs,
    OperatorRunFocusService runFocus,
    OperatorResearchService operatorResearch) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Start(Guid runId, CancellationToken ct)
    {
        var runExists = await db.AnalysisRuns.AnyAsync(r => r.Id == runId, ct);
        if (!runExists)
            return NotFound();

        var existingPages = await db.CompetitorPages.AsNoTracking().CountAsync(p => p.RunId == runId, ct);
        if (existingPages > 0)
        {
            var run = await db.AnalysisRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
            if (run is not null && run.GapTopics.Count == 0)
            {
                try
                {
                    await runFocus.TryCompleteResearchFocusAsync(runId, ct);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new
                    {
                        error = ex.Message,
                        crawlStatus = CompetitorCrawlStatuses.Failed,
                        competitorSaved = false,
                    });
                }

                run = await db.AnalysisRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
                if (run is null || run.GapTopics.Count == 0)
                {
                    return StatusCode(500, new
                    {
                        error = "Research pack assembly did not persist gap themes.",
                        crawlStatus = CompetitorCrawlStatuses.Failed,
                        competitorSaved = false,
                    });
                }
            }

            var stats = await CompetitorCrawlStatsQuery.LoadAsync(db, runId, ct);
            var focus = await operatorResearch.GetResearchFocusAsync(runId, ct);
            var researchReady = focus?.ResearchReady ?? false;
            return Ok(new
            {
                crawlStatus = researchReady ? CompetitorCrawlStatuses.Complete : CompetitorCrawlStatuses.PagesSaved,
                competitorSaved = researchReady,
                totalPages = stats.TotalPages,
                domainCount = stats.DomainCount,
                domains = stats.Domains.Select(d => new { domain = d.Domain, pagesCrawled = d.PagesCrawled }),
                message = researchReady
                    ? $"Saved {stats.TotalPages} pages across {stats.DomainCount} competitor domains. Research pack ready."
                    : $"Saved {stats.TotalPages} pages across {stats.DomainCount} competitor domains. Research pack assembly did not complete.",
            });
        }

        var state = await crawlJobs.GetStateAsync(db, runId, ct);
        if (state.Status == CompetitorCrawlJobStatus.Running)
        {
            return Accepted(new
            {
                crawlStatus = CompetitorCrawlStatuses.Running,
                runId,
                message = "Competitor crawl is already running.",
            });
        }

        if (!await crawlJobs.TryStartAsync(db, runId, ct))
        {
            return Accepted(new
            {
                crawlStatus = CompetitorCrawlStatuses.Running,
                runId,
                message = "Competitor crawl is already running.",
            });
        }

        return Accepted(new
        {
            crawlStatus = CompetitorCrawlStatuses.Running,
            runId,
            message = "Competitor crawl started. This may take several minutes.",
        });
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid runId, CancellationToken ct)
    {
        var runExists = await db.AnalysisRuns.AnyAsync(r => r.Id == runId, ct);
        if (!runExists)
            return NotFound();

        var jobState = await crawlJobs.GetStateAsync(db, runId, ct);
        var pages = await db.CompetitorPages
            .AsNoTracking()
            .Where(p => p.RunId == runId)
            .Include(p => p.Headings)
            .Include(p => p.MetaTags)
            .Include(p => p.JsonLdBlocks)
            .OrderBy(p => p.Domain)
            .ThenBy(p => p.DepthFromSeed)
            .ThenBy(p => p.Url)
            .ToListAsync(ct);

        var gapTopicCount = await db.AnalysisRuns.AsNoTracking()
            .Where(r => r.Id == runId)
            .Select(r => r.GapTopics.Count)
            .FirstOrDefaultAsync(ct);
        var researchPackReady = pages.Count > 0 && gapTopicCount > 0;
        var crawlStatus = ResolveCrawlStatus(jobState, pages.Count, researchPackReady);
        var competitorSaved = researchPackReady;

        if (jobState.Status == CompetitorCrawlJobStatus.Failed && pages.Count == 0)
        {
            return Ok(new
            {
                crawlStatus = CompetitorCrawlStatuses.Failed,
                totalPages = 0,
                domainCount = 0,
                competitorSaved = false,
                message = jobState.Message ?? "Competitor crawl failed.",
                qualityWarnings = Array.Empty<string>(),
            });
        }

        if (jobState.Status == CompetitorCrawlJobStatus.Complete && competitorSaved)
        {
            var domainCount = pages.Select(p => p.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            return Ok(new
            {
                crawlStatus = CompetitorCrawlStatuses.Complete,
                competitorSaved = true,
                totalPages = pages.Count,
                domainCount,
                message = "Competitor crawl complete. Research pack ready.",
            });
        }

        if (jobState.Status == CompetitorCrawlJobStatus.Complete && pages.Count > 0 && !researchPackReady)
        {
            return Ok(new
            {
                crawlStatus = CompetitorCrawlStatuses.PagesSaved,
                competitorSaved = false,
                totalPages = pages.Count,
                domainCount = pages.Select(p => p.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                message = jobState.Message
                    ?? "Competitor pages saved but research pack assembly did not complete.",
                qualityWarnings = Array.Empty<string>(),
            });
        }

        if (jobState.Status == CompetitorCrawlJobStatus.PagesSaved)
        {
            return Ok(new
            {
                crawlStatus = CompetitorCrawlStatuses.PagesSaved,
                competitorSaved = false,
                totalPages = pages.Count,
                domainCount = pages.Select(p => p.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                message = jobState.Message
                    ?? "Competitor pages saved but research pack assembly did not complete.",
                qualityWarnings = Array.Empty<string>(),
            });
        }

        if (jobState.Status == CompetitorCrawlJobStatus.Running)
        {
            var runningDomainCount = await db.CompetitorPages.AsNoTracking()
                .Where(p => p.RunId == runId)
                .Select(p => p.Domain)
                .Distinct()
                .CountAsync(ct);

            return Ok(new
            {
                crawlStatus = CompetitorCrawlStatuses.Running,
                totalPages = pages.Count,
                domainCount = runningDomainCount,
                competitorSaved = false,
                message = "Competitor crawl is still running.",
            });
        }

        if (competitorSaved)
        {
            var domainCount = pages.Select(p => p.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            return Ok(new
            {
                crawlStatus = CompetitorCrawlStatuses.Complete,
                competitorSaved = true,
                totalPages = pages.Count,
                domainCount,
                message = "Competitor crawl complete. Research pack ready.",
            });
        }

        if (pages.Count > 0 && !researchPackReady)
        {
            return Ok(new
            {
                crawlStatus = CompetitorCrawlStatuses.PagesSaved,
                competitorSaved = false,
                totalPages = pages.Count,
                domainCount = pages.Select(p => p.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                message = jobState.Message
                    ?? "Competitor pages saved but research pack assembly did not complete.",
                qualityWarnings = Array.Empty<string>(),
            });
        }

        var byDomain = pages
            .GroupBy(p => p.Domain)
            .Select(g => new
            {
                Domain = g.Key,
                SeedRankAbsolute = g.Min(p => p.SeedRankAbsolute),
                PagesCrawled = g.Count(),
                Pages = g.Select(p => new
                {
                    p.Id,
                    p.Url,
                    p.CanonicalUrl,
                    p.HttpStatus,
                    p.DepthFromSeed,
                    p.CrawledAt,
                    Headings = p.Headings.OrderBy(h => h.Sequence).Select(h => new { h.Level, h.Text, h.Sequence }),
                    MetaTags = p.MetaTags.Select(m => new { m.NameOrProperty, m.Content }),
                    JsonLd = p.JsonLdBlocks.Select(j => new { j.ParsedType, j.RawJson })
                })
            })
            .ToList();

        return Ok(new
        {
            crawlStatus,
            totalPages = pages.Count,
            domainCount = byDomain.Count,
            competitorSaved,
            domains = byDomain,
            message = crawlStatus == CompetitorCrawlStatuses.Running
                ? "Competitor crawl is still running."
                : jobState.Message,
        });
    }

    [HttpGet("progress-catchup")]
    public async Task<IActionResult> ProgressCatchup(Guid runId, [FromQuery] long lastSeq, CancellationToken ct)
    {
        var runExists = await db.AnalysisRuns.AnyAsync(r => r.Id == runId, ct);
        if (!runExists)
            return NotFound();

        if (lastSeq < 0)
            return BadRequest(new { error = "lastSeq must be zero or greater." });

        var missed = await db.CompetitorCrawlProgressLogs
            .AsNoTracking()
            .Where(l => l.RunId == runId && l.Id > lastSeq)
            .OrderBy(l => l.Id)
            .Select(l => new
            {
                sequenceNumber = l.Id,
                payload = l.Payload,
            })
            .ToListAsync(ct);

        return Ok(missed);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(Guid runId, CancellationToken ct)
    {
        var runExists = await db.AnalysisRuns.AnyAsync(r => r.Id == runId, ct);
        if (!runExists)
            return NotFound();

        var jobState = await crawlJobs.GetStateAsync(db, runId, ct);
        var stats = await CompetitorCrawlStatsQuery.LoadAsync(db, runId, ct);
        var totalPages = stats.TotalPages;
        var domainCount = stats.DomainCount;
        var gapTopicCount = await db.AnalysisRuns.AsNoTracking()
            .Where(r => r.Id == runId)
            .Select(r => r.GapTopics.Count)
            .FirstOrDefaultAsync(ct);
        var researchPackReady = totalPages > 0 && gapTopicCount > 0;
        var competitorSaved = researchPackReady;
        var crawlStatus = ResolveCrawlStatus(jobState, totalPages, researchPackReady);

        if (jobState.Status == CompetitorCrawlJobStatus.Complete && totalPages > 0 && !researchPackReady)
            crawlStatus = CompetitorCrawlStatuses.PagesSaved;
        else if (jobState.Status == CompetitorCrawlJobStatus.Complete && researchPackReady)
            crawlStatus = CompetitorCrawlStatuses.Complete;
        else if (jobState.Status == CompetitorCrawlJobStatus.PagesSaved)
            crawlStatus = CompetitorCrawlStatuses.PagesSaved;
        else if (jobState.Status == CompetitorCrawlJobStatus.Failed && totalPages == 0)
            crawlStatus = CompetitorCrawlStatuses.Failed;
        else if (jobState.Status == CompetitorCrawlJobStatus.Running)
            crawlStatus = CompetitorCrawlStatuses.Running;

        var domainSummaries = stats.Domains
            .Select(d => new { domain = d.Domain, pagesCrawled = d.PagesCrawled })
            .ToList();

        var message = researchPackReady
            ? $"Saved {totalPages} pages across {domainCount} competitor domains. Research pack ready."
            : totalPages > 0
                ? jobState.Message
                  ?? "Competitor pages saved but research pack assembly did not complete."
                : jobState.Status == CompetitorCrawlJobStatus.Running
                    ? $"Crawled {totalPages} pages across {domainCount} domains so far."
                    : jobState.Message;

        return Ok(new
        {
            crawlStatus,
            competitorSaved = researchPackReady,
            totalPages,
            domainCount,
            domains = domainSummaries,
            message,
            qualityWarnings = Array.Empty<string>(),
        });
    }

    private static string ResolveCrawlStatus(
        CompetitorCrawlJobState jobState,
        int pageCount,
        bool researchPackReady) =>
        jobState.Status switch
        {
            CompetitorCrawlJobStatus.Running => CompetitorCrawlStatuses.Running,
            CompetitorCrawlJobStatus.Failed => CompetitorCrawlStatuses.Failed,
            CompetitorCrawlJobStatus.PagesSaved => CompetitorCrawlStatuses.PagesSaved,
            CompetitorCrawlJobStatus.Complete => researchPackReady
                ? CompetitorCrawlStatuses.Complete
                : CompetitorCrawlStatuses.PagesSaved,
            _ => pageCount > 0 && researchPackReady
                ? CompetitorCrawlStatuses.Complete
                : pageCount > 0
                    ? CompetitorCrawlStatuses.PagesSaved
                    : CompetitorCrawlStatuses.Idle,
        };
}
