using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Hubs;
using GeekSeoBackend.Services.NicheExtraction;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services;

/// <summary>
/// Runs the full topic-extraction pipeline on each competitor domain — same
/// pipeline used on the project site — and stores their discovered pillars.
/// Triggered separately after the initial niche analysis completes.
/// </summary>
public sealed class CompetitorAnalysisService(
    INicheProfileRepository profileRepo,
    SchemaOrgExtractor schemaExtractor,
    SitemapExtractor sitemapExtractor,
    NavMenuExtractor navMenuExtractor,
    HomepageHeadingsExtractor headingsExtractor,
    PageContentExtractor pageContentExtractor,
    SitePageCrawler sitePageCrawler,
    InternalLinkExtractor internalLinkExtractor,
    UrlPatternExtractor urlPatternExtractor,  // instance — injected
    PillarSelector pillarSelector,
    IHubContext<SeoRealtimeHub> hub,
    ILogger<CompetitorAnalysisService> logger)
{
    private const int MaxPagesPerCompetitor = 50;

    public async Task AnalyzeAsync(
        Guid profileId,
        Guid userId,
        IReadOnlyList<NicheCompetitor> competitors,
        IBrowser? browser,
        CancellationToken ct)
    {
        var total = competitors.Count;
        var done = 0;

        await PushProgress(profileId, userId, 0, total, "Starting competitor analysis…", ct);

        foreach (var competitor in competitors)
        {
            ct.ThrowIfCancellationRequested();
            done++;
            var domain = competitor.Domain;

            await PushProgress(profileId, userId, done, total,
                $"Analyzing {domain} ({done}/{total})…", ct);

            try
            {
                var siteUrl = $"https://{domain}";

                var schema = await schemaExtractor.ExtractAsync(siteUrl, browser, ct);
                var sitemap = await sitemapExtractor.ExtractAsync(siteUrl, ct);
                var nav = browser is not null
                    ? await navMenuExtractor.ExtractAsync(siteUrl, browser, ct)
                    : new NavMenuData([], "skipped");
                var headings = await headingsExtractor.ExtractAsync(siteUrl, browser, ct);
                var pageContent = await pageContentExtractor.ExtractAsync(siteUrl, browser, ct);
                var crawlData = await sitePageCrawler.CrawlAsync(
                    siteUrl, sitemap.SampleUrls, browser, ct, maxPages: MaxPagesPerCompetitor);
                var internalLinks = internalLinkExtractor.Extract(crawlData, domain);
                var urlPatterns = urlPatternExtractor.Extract(
                    crawlData.Pages.Select(p => p.Url).Concat(sitemap.SampleUrls).Distinct().ToList(),
                    domain);

                var pool = TopicCandidatePoolBuilder.Build(
                    schema, sitemap, nav, headings, pageContent, internalLinks, urlPatterns);
                var profile = pillarSelector.Select(pool, schema.AreaServed.ToList());

                var pillars = profile.SelectedPillars
                    .Select(c => new CompetitorPillarDto(c.Name, c.Slug, c.Evidence.FirstOrDefault()?.Source ?? "schema", (double)c.Confidence))
                    .ToList();

                competitor.PagesCrawled = crawlData.PagesFetched;
                competitor.AvgWordCount = EstimateAvgWordCount(crawlData);
                competitor.HasFaqSchema = schema.ServiceNames.Any() || pillars.Any();
                competitor.Description = schema.Description;
                competitor.BrandName = schema.BrandName;
                competitor.ServicesJson = SerializeList(schema.ServiceNames);
                competitor.KnowsAboutJson = SerializeList(schema.KnowsAboutTopics);
                competitor.AreaServedJson = SerializeList(schema.AreaServed);
                competitor.SameAsJson = SerializeList(schema.SameAsUrls);
                competitor.PillarsJson = pillars.Count > 0 ? JsonSerializer.Serialize(pillars) : null;
                competitor.CompetitorAnalyzedAt = DateTimeOffset.UtcNow;

                var updateResult = await profileRepo.UpdateCompetitorInsightsAsync(competitor, ct);
                if (!updateResult.IsSuccess)
                    logger.LogWarning("Failed to save competitor insights for {Domain}: {Error}", domain, updateResult.Error);

                await PushProgress(profileId, userId, done, total,
                    $"{domain}: {pillars.Count} pillar(s) found, {crawlData.PagesFetched} page(s) crawled.", ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Competitor analysis failed for {Domain}", domain);
                await PushProgress(profileId, userId, done, total,
                    $"{domain}: failed — {ex.Message}", ct);
            }
        }

        await PushProgress(profileId, userId, total, total,
            $"Competitor analysis complete — {total} site(s) analyzed.", ct);
    }

    private async Task PushProgress(
        Guid profileId, Guid userId, int done, int total, string message, CancellationToken ct)
    {
        try
        {
            await hub.Clients.Group($"niche-{profileId}").SendAsync(
                "CompetitorAnalysisProgress",
                new { ProfileId = profileId, Done = done, Total = total, Message = message },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SignalR push failed for competitor analysis progress");
        }
    }

    private static int EstimateAvgWordCount(SiteCrawlData crawl)
    {
        if (crawl.Pages.Count == 0) return 0;
        var total = crawl.Pages.Sum(p =>
            p.Html.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length);
        return total / crawl.Pages.Count;
    }

    private static string? SerializeList(IReadOnlyList<string> list) =>
        list is { Count: > 0 } ? JsonSerializer.Serialize(list) : null;

    private sealed record CompetitorPillarDto(string Name, string Slug, string Source, double Confidence);
}
