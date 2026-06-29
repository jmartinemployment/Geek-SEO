using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Crawling;
using SiteAnalyzer2.Services.Utilities;
using System.Collections.Concurrent;

namespace SiteAnalyzer2.Services.Pipeline;

public class PageFetchService(AppDbContext db, IHttpClientFactory httpClientFactory, Parsing.PageExtractionService extractionService)
{
    private static readonly HtmlParser HtmlParser = new();

    public async Task<int> RunFetchStageAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var project = run.Project;
        var client = httpClientFactory.CreateClient(nameof(PageFetchService));
        client.Timeout = TimeSpan.FromMinutes(2);

        var includedSerpUrls = await db.SerpItems
            .Where(i => i.RunId == runId
                && i.Type == Domain.SerpItemTypes.Organic
                && !i.Ads
                && i.FilterStatus == FilterStatus.Included)
            .Select(i => i.Url!)
            .ToListAsync(ct);

        var pages = new ConcurrentDictionary<string, Page>(StringComparer.OrdinalIgnoreCase);
        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(run.TargetSiteUrl));

        await CrawlTargetSiteAsync(run, project, client, targetDomain, pages, ct);
        await FetchUrlsAsync(run, client, includedSerpUrls, isTargetSite: false, depth: null, pages, ct);

        return await PersistNewPagesAsync(pages, ct);
    }

    /// <summary>Operator path: crawl Project URL only (no SERP competitor URLs in pages table).</summary>
    public async Task<int> RunTargetSiteFetchAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        await ClearTargetPagesAsync(runId, ct);

        var client = httpClientFactory.CreateClient(nameof(PageFetchService));
        client.Timeout = TimeSpan.FromMinutes(2);

        var pages = new ConcurrentDictionary<string, Page>(StringComparer.OrdinalIgnoreCase);
        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(run.TargetSiteUrl));
        await CrawlTargetSiteAsync(run, run.Project, client, targetDomain, pages, ct);

        return await PersistNewPagesAsync(pages, ct);
    }

    private async Task<int> PersistNewPagesAsync(ConcurrentDictionary<string, Page> pages, CancellationToken ct)
    {
        var newPages = pages.Values.Where(p => p.Id == Guid.Empty).ToList();
        foreach (var page in newPages)
            page.Id = Guid.NewGuid();

        await db.Pages.AddRangeAsync(newPages, ct);
        await db.SaveChangesAsync(ct);
        return newPages.Count;
    }

    private async Task ClearTargetPagesAsync(Guid runId, CancellationToken ct)
    {
        var targetPages = await db.Pages.Where(p => p.RunId == runId && p.IsTargetSite).ToListAsync(ct);
        if (targetPages.Count == 0)
            return;

        db.Pages.RemoveRange(targetPages);
        await db.SaveChangesAsync(ct);
    }

    private async Task CrawlTargetSiteAsync(
        AnalysisRun run,
        Project project,
        HttpClient client,
        string targetDomain,
        ConcurrentDictionary<string, Page> pages,
        CancellationToken ct)
    {
        if (!Uri.TryCreate(run.TargetSiteUrl, UriKind.Absolute, out var homepageUri))
            return;

        var patterns = await db.CrawlPriorityUrlPatterns
            .Select(p => p.Pattern)
            .ToListAsync(ct);

        var highPriorityQueue = new Queue<(Uri Url, int Depth)>();
        var normalPriorityQueue = new Queue<(Uri Url, int Depth)>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var navLinkUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        EnqueueUrl(homepageUri, 0, isHighPriority: false);

        while ((highPriorityQueue.Count > 0 || normalPriorityQueue.Count > 0)
               && pages.Values.Count(p => p.IsTargetSite) < project.MaxCrawlPages)
        {
            ct.ThrowIfCancellationRequested();

            var (url, depth) = highPriorityQueue.Count > 0
                ? highPriorityQueue.Dequeue()
                : normalPriorityQueue.Dequeue();

            var normalized = CrawlPriorityMatcher.NormalizeUrl(url);
            if (pages.ContainsKey(normalized))
                continue;

            var page = await FetchPageAsync(run, client, url, isTargetSite: true, depth, ct);
            if (page == null)
                continue;

            pages[normalized] = page;

            if (depth == 0 && !string.IsNullOrWhiteSpace(page.HtmlContent))
            {
                foreach (var navUrl in ExtractNavLinkUrls(page.HtmlContent, url))
                    navLinkUrls.Add(navUrl);
            }

            if (depth >= project.MaxCrawlDepth || string.IsNullOrWhiteSpace(page.HtmlContent))
                continue;

            foreach (var link in ExtractLinksFromHtml(page.HtmlContent, url, targetDomain))
            {
                if (!Uri.TryCreate(link, UriKind.Absolute, out var linkUri))
                    continue;

                var isHighPriority = CrawlPriorityMatcher.IsPriorityUrl(linkUri, patterns, navLinkUrls);
                EnqueueUrl(linkUri, depth + 1, isHighPriority);
            }
        }

        return;

        void EnqueueUrl(Uri url, int depth, bool isHighPriority)
        {
            var normalizedUrl = CrawlPriorityMatcher.NormalizeUrl(url);
            if (!visited.Add(normalizedUrl))
                return;

            if (isHighPriority)
                highPriorityQueue.Enqueue((url, depth));
            else
                normalPriorityQueue.Enqueue((url, depth));
        }
    }

    private async Task FetchUrlsAsync(
        AnalysisRun run,
        HttpClient client,
        IEnumerable<string> urls,
        bool isTargetSite,
        int? depth,
        ConcurrentDictionary<string, Page> pages,
        CancellationToken ct)
    {
        foreach (var urlText in urls.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();

            if (!Uri.TryCreate(urlText, UriKind.Absolute, out var url))
                continue;

            var normalized = NormalizeUrl(url);
            if (pages.ContainsKey(normalized))
                continue;

            var page = await FetchPageAsync(run, client, url, isTargetSite, depth, ct);
            if (page != null)
                pages[normalized] = page;
        }
    }

    private async Task<Page?> FetchPageAsync(
        AnalysisRun run,
        HttpClient client,
        Uri url,
        bool isTargetSite,
        int? depth,
        CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            var html = response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(ct)
                : null;

            return new Page
            {
                ProjectId = run.ProjectId,
                RunId = run.Id,
                Url = NormalizeUrl(url),
                FetchMode = FetchMode.Http,
                HttpStatus = (int)response.StatusCode,
                IsTargetSite = isTargetSite,
                DepthFromHomepage = depth,
                HtmlContent = html
            };
        }
        catch (HttpRequestException)
        {
            return new Page
            {
                ProjectId = run.ProjectId,
                RunId = run.Id,
                Url = NormalizeUrl(url),
                FetchMode = FetchMode.Http,
                HttpStatus = 0,
                IsTargetSite = isTargetSite,
                DepthFromHomepage = depth,
                HtmlContent = null
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private IEnumerable<string> ExtractLinksFromHtml(string html, Uri baseUrl, string siteRegistrableDomain)
    {
        var result = extractionService.Extract(html, baseUrl, siteRegistrableDomain);
        return result.InternalLinks.Select(l => l.AbsoluteUrl);
    }

    private static IEnumerable<string> ExtractNavLinkUrls(string html, Uri baseUrl)
    {
        var document = HtmlParser.ParseDocument(html);
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var anchor in document.QuerySelectorAll(
                     "nav a[href], header a[href], [role='navigation'] a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(baseUrl, href, out var absolute))
                continue;

            urls.Add(CrawlPriorityMatcher.NormalizeUrl(absolute));
        }

        return urls;
    }

    private static string NormalizeUrl(Uri uri) =>
        uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
}
