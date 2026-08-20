using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services.SiteExtraction;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

/// <summary>
/// Content Creator Through Coverage: extract in memory, SignalR to the signed-in user,
/// one persist at the end (profile id is born on that insert). Failure saves nothing.
/// </summary>
public sealed class ThroughCoverageMemoryRunner(
    ISiteAnalysisProfileRepository profileRepo,
    SchemaOrgExtractor schemaExtractor,
    SitemapGenerator sitemapGenerator,
    NavMenuExtractor navMenuExtractor,
    HomepageHeadingsExtractor headingsExtractor,
    PageContentExtractor pageContentExtractor,
    SitePageCrawler sitePageCrawler,
    InternalLinkExtractor internalLinkExtractor,
    UrlPatternExtractor urlPatternExtractor,
    SiteAnalysisProgressNotifier progressNotifier,
    ILogger<ThroughCoverageMemoryRunner> logger)
{
    public async Task RunAsync(ThroughCoverageJob job, IBrowser? browser, CancellationToken ct)
    {
        var userId = job.UserId;
        var domain = SiteUrlNormalizer.Normalize(job.Domain);
        var total = SiteAnalyzerStepCatalog.ThroughCoverage.Count;
        var stepNumber = 0;

        try
        {
            await Push(userId, Guid.Empty, "schema", "processing", "Starting crawl…", 0, total, ct);

            var schemaData = await schemaExtractor.ExtractAsync(domain, browser, ct);
            stepNumber = 1;
            await Push(userId, Guid.Empty, "schema", "processing", SchemaMessage(schemaData), stepNumber, total, ct);

            var generated = await sitemapGenerator.GenerateAsync(domain, browser, ct);
            if (!TryGetOrigin(domain, out var origin))
                throw new InvalidOperationException($"Sitemap generation found no pages for {domain} — invalid site URL.");
            var validInventory = generated.Inventory
                .Where(u =>
                    (string.Equals(u.SourceType, "sitemap", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(u.SourceType, "generated", StringComparison.OrdinalIgnoreCase))
                    && IsSameOrigin(u.Url, origin))
                .GroupBy(u => $"{u.Url}␟{u.SourceType}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            if (validInventory.Count == 0)
                throw new InvalidOperationException($"Sitemap generation found no pages for {domain}.");
            var discovered = validInventory
                .Select(u => new SiteAnalysisProfileDiscoveredUrlWrite(u.Url, u.SourceType))
                .ToList();
            stepNumber = 2;
            await Push(
                userId, Guid.Empty, "site_urls", "processing",
                $"Sitemap generated: {validInventory.Count} URL(s).",
                stepNumber, total, ct);

            var navData = browser is not null
                ? await navMenuExtractor.ExtractAsync(domain, browser, ct)
                : new NavMenuData([], "skipped");
            var navLinks = navData.Pillars.Select((pillar, index) => new SiteAnalysisProfileNavigationLinkWrite(
                domain,
                pillar.PageUrl ?? $"{domain.TrimEnd('/')}/{pillar.Slug.TrimStart('/')}",
                pillar.Name,
                navData.ExtractMethod,
                index)).ToList();
            stepNumber = 3;
            await Push(userId, Guid.Empty, "nav", "processing", NavMessage(navData), stepNumber, total, ct);

            await headingsExtractor.ExtractAsync(domain, browser, ct);
            stepNumber = 4;
            await Push(userId, Guid.Empty, "headings", "processing", "Headings: homepage scan complete.", stepNumber, total, ct);

            var pageContent = await pageContentExtractor.ExtractAsync(domain, browser, ct);
            var pageContentWrite = SiteAnalyzerStepRelationalLoader.ToPageContentWrite(domain, pageContent);
            stepNumber = 5;
            await Push(userId, Guid.Empty, "page_content", "processing", PageContentMessage(pageContent), stepNumber, total, ct);

            var inventoryUrls = discovered.Select(d => d.Url).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var crawlData = await sitePageCrawler.CrawlAsync(domain, inventoryUrls, browser, ct);
            var fetchedSet = new HashSet<string>(crawlData.Pages.Select(p => p.Url), StringComparer.OrdinalIgnoreCase);
            var crawlMessage = SiteCrawlInventoryCompleteness.Evaluate(inventoryUrls, fetchedSet, out var missing);
            if (missing.Count > 0)
            {
                logger.LogWarning(
                    "Site crawl for {Domain}: excluding {MissingCount} inventory URL(s) not fetched",
                    domain, missing.Count);
            }

            // Index-time canonical consolidation — see SiteAnalysisStepExecutionService.
            var documentPages = CanonicalPageConsolidator
                .Consolidate(crawlData.Pages.Where(p => p.HasDocument).ToList());
            var documentUrls = documentPages.Select(p => p.Url).ToList();
            var treeWrites = documentPages
                .Select(page => new SiteAnalysisPageSectionTreeWrite(
                    page.Url,
                    JsonSerializer.Serialize(PageSectionTreeBuilder.Build(page.Html))))
                .ToList();

            var crawlDiscovered = discovered
                .Where(x => !string.Equals(x.SourceType, "crawl", StringComparison.OrdinalIgnoreCase))
                .Concat(documentUrls
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(url => new SiteAnalysisProfileDiscoveredUrlWrite(url, "crawl")))
                .ToList();

            stepNumber = 6;
            await Push(userId, Guid.Empty, "site_crawl", "processing", crawlMessage, stepNumber, total, ct);

            var internalLinks = internalLinkExtractor.Extract(crawlData, domain);
            stepNumber = 7;
            await Push(
                userId, Guid.Empty, "internal_links", "processing",
                $"Internal links: {internalLinks.Links.Count} link(s).",
                stepNumber, total, ct);

            var patternUrls = crawlDiscovered.Select(d => d.Url)
                .Concat(documentUrls)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var urlPatterns = urlPatternExtractor.Extract(patternUrls, domain);
            stepNumber = 8;
            await Push(
                userId, Guid.Empty, "url_patterns", "processing",
                $"URL patterns: {urlPatterns.Topics.Count} topic(s).",
                stepNumber, total, ct);

            var structure = SiteAnalyzerStepRelationalLoader.ToSiteStructureWrite(
                crawlData, internalLinks, urlPatterns, forceDocumentWrite: true);

            var persist = await profileRepo.PersistThroughCoverageAsync(
                new ThroughCoveragePersistRequest(
                    job.ProjectId,
                    domain,
                    BuildSchemaSignals(schemaData),
                    crawlDiscovered,
                    navLinks,
                    pageContentWrite,
                    treeWrites,
                    structure),
                ct);
            if (!persist.IsSuccess || persist.Value == Guid.Empty)
                throw new InvalidOperationException(persist.Error ?? "Failed to persist crawl.");

            var profileId = persist.Value;
            logger.LogInformation("Through coverage persisted as profile {ProfileId} for {Domain}", profileId, domain);
            await Push(userId, profileId, "complete", "complete", "Crawl saved.", total, total, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Through coverage failed for {Domain}", domain);
            var message = ex is OperationCanceledException ? "Analysis timed out." : ex.Message;
            await Push(userId, Guid.Empty, "failed", "failed", message, stepNumber, total, ct);
        }
    }

    private Task Push(
        Guid userId,
        Guid profileId,
        string slug,
        string status,
        string message,
        int stepNumber,
        int totalSteps,
        CancellationToken ct) =>
        progressNotifier.PushAsync(profileId, userId, slug, status, message, stepNumber, totalSteps, ct);

    private static List<SiteAnalysisProfileSchemaSignalWrite> BuildSchemaSignals(SchemaOrgData schemaData)
    {
        var signals = new List<SiteAnalysisProfileSchemaSignalWrite>();
        var order = 0;
        signals.AddRange(schemaData.ServiceNames.Select(value =>
            new SiteAnalysisProfileSchemaSignalWrite("service", "name", value, null, order++)));
        signals.AddRange(schemaData.KnowsAboutTopics.Select(value =>
            new SiteAnalysisProfileSchemaSignalWrite("thing", "knowsAbout", value, null, order++)));
        signals.AddRange(schemaData.OfferCatalogTopics.Select(value =>
            new SiteAnalysisProfileSchemaSignalWrite("offer_catalog", "serviceType", value, null, order++)));
        signals.AddRange(schemaData.AreaServed.Select(value =>
            new SiteAnalysisProfileSchemaSignalWrite("organization", "areaServed", value, null, order++)));
        signals.AddRange(schemaData.SameAsUrls.Select(value =>
            new SiteAnalysisProfileSchemaSignalWrite("organization", "sameAs", value, value, order++)));
        if (!string.IsNullOrWhiteSpace(schemaData.Description))
            signals.Add(new SiteAnalysisProfileSchemaSignalWrite("organization", "description", schemaData.Description, null, order++));
        if (!string.IsNullOrWhiteSpace(schemaData.BrandName))
            signals.Add(new SiteAnalysisProfileSchemaSignalWrite("organization", "brandName", schemaData.BrandName, null, order++));
        return signals;
    }

    private static string SchemaMessage(SchemaOrgData schemaData) =>
        schemaData.ServiceNames.Count > 0
            ? $"Found {schemaData.ServiceNames.Count} schema topic(s) in homepage JSON-LD."
            : "Schema.org step complete — no service topics on homepage.";

    private static string NavMessage(NavMenuData navData) =>
        navData.ExtractMethod switch
        {
            "skipped" => "Navigation step skipped — browser unavailable.",
            _ => $"Navigation: {navData.Pillars.Count} link groups ({navData.ExtractMethod}).",
        };

    private static string PageContentMessage(PageContentData pageContent)
    {
        var parts = new List<string>();
        if (pageContent.VerticalTopics.Count > 0)
            parts.Add($"{pageContent.VerticalTopics.Count} H2/H3 vertical section(s)");
        if (pageContent.ServicePhrases.Count > 0)
            parts.Add($"{pageContent.ServicePhrases.Count} body phrase(s)");
        return parts.Count > 0
            ? $"Page content: {string.Join(", ", parts)} from homepage."
            : "Page content: no additional service phrases on homepage.";
    }

    private static bool TryGetOrigin(string siteUrl, out string origin)
    {
        origin = string.Empty;
        try
        {
            origin = new Uri(SiteUrlNormalizer.Normalize(siteUrl)).GetLeftPart(UriPartial.Authority);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsSameOrigin(string url, string origin)
    {
        try
        {
            return new Uri(url).GetLeftPart(UriPartial.Authority)
                .Equals(origin, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
