using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.Parsing;
using SiteAnalyzer2.Services.Utilities;

namespace SiteAnalyzer2.Services.Pipeline;

public class LinkGraphBuilderService(AppDbContext db, PageExtractionService extractionService)
{
    public async Task<(int InternalLinks, int CrossRunLinks)> RunGraphStageAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException($"Run {runId} not found.");

        var pages = await db.Pages
            .Where(p => p.RunId == runId && p.HtmlContent != null)
            .ToListAsync(ct);

        var urlToPage = pages.ToDictionary(p => NormalizeUrl(p.Url), p => p, StringComparer.OrdinalIgnoreCase);
        var targetDomain = DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(run.TargetSiteUrl));

        var internalLinks = new List<InternalLink>();
        var crossRunLinks = new List<CrossRunLink>();

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.HtmlContent))
                continue;

            if (!Uri.TryCreate(page.Url, UriKind.Absolute, out var pageUrl))
                continue;

            var pageDomain = page.IsTargetSite
                ? targetDomain
                : DomainHelper.GetRegistrableDomain(DomainHelper.GetHostFromUrl(page.Url));

            var extraction = extractionService.Extract(page.HtmlContent, pageUrl, pageDomain);

            foreach (var link in extraction.InternalLinks)
            {
                if (!urlToPage.TryGetValue(NormalizeUrl(link.AbsoluteUrl), out var toPage))
                    continue;

                if (page.IsTargetSite && toPage.IsTargetSite)
                {
                    internalLinks.Add(new InternalLink
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = run.ProjectId,
                        RunId = run.Id,
                        FromPageId = page.Id,
                        ToPageId = toPage.Id,
                        Href = link.Href,
                        AnchorText = link.AnchorText
                    });
                }

                var isInternalToDomain = DomainHelper.HostsMatch(
                    DomainHelper.GetHostFromUrl(page.Url),
                    DomainHelper.GetHostFromUrl(toPage.Url));

                crossRunLinks.Add(new CrossRunLink
                {
                    Id = Guid.NewGuid(),
                    ProjectId = run.ProjectId,
                    RunId = run.Id,
                    FromPageId = page.Id,
                    ToPageId = toPage.Id,
                    IsInternalToDomain = isInternalToDomain,
                    Href = link.Href
                });
            }
        }

        await db.InternalLinks.AddRangeAsync(internalLinks, ct);
        await db.CrossRunLinks.AddRangeAsync(crossRunLinks, ct);
        await db.SaveChangesAsync(ct);

        return (internalLinks.Count, crossRunLinks.Count);
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.TrimEnd('/').ToLowerInvariant();

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
    }
}
