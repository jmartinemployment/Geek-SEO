using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeoBackend.Services.SiteExtraction;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

public sealed class SiteAnalyzerStepExecutionService(
    ISiteAnalysisProfileRepository profileRepo,
    SchemaOrgExtractor schemaExtractor,
    SitemapGenerator sitemapGenerator,
    NavMenuExtractor navMenuExtractor,
    HomepageHeadingsExtractor headingsExtractor,
    PageContentExtractor pageContentExtractor,
    SitePageCrawler sitePageCrawler,
    InternalLinkExtractor internalLinkExtractor,
    UrlPatternExtractor urlPatternExtractor,
    ILogger<SiteAnalyzerStepExecutionService> logger)
{
    public Task<SiteAnalysisStepLogEntry> RunAsync(
        string slug,
        Guid profileId,
        Guid userId,
        string domain,
        IBrowser? browser,
        CancellationToken ct) =>
        slug switch
        {
            "schema" => RunSchemaAsync(profileId, domain, browser, ct),
            "site_urls" => RunSiteUrlsAsync(profileId, domain, browser, ct),
            "nav" => RunNavAsync(profileId, domain, browser, ct),
            "headings" => RunHeadingsAsync(profileId, domain, browser, ct),
            "page_content" => RunPageContentAsync(profileId, domain, browser, ct),
            "site_crawl" => RunSiteCrawlAsync(profileId, domain, browser, ct),
            "internal_links" => RunInternalLinksAsync(profileId, domain, ct),
            "url_patterns" => RunUrlPatternsAsync(profileId, domain, ct),
            "complete" => RunCompleteAsync(profileId, ct),
            _ => throw new InvalidOperationException($"Unknown site analysis step '{slug}'."),
        };

    private async Task<SiteAnalysisStepLogEntry> RunSchemaAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        var schemaData = await schemaExtractor.ExtractAsync(domain, browser, ct);
        var persistSignals = await profileRepo.ReplaceSchemaSignalsAsync(
            profileId,
            BuildSchemaSignals(schemaData),
            ct);
        if (!persistSignals.IsSuccess)
            throw new InvalidOperationException(persistSignals.Error ?? "Failed to persist schema signals.");
        var message = schemaData.ServiceNames.Count > 0
            ? $"Found {schemaData.ServiceNames.Count} schema topic(s) in homepage JSON-LD ({schemaData.KnowsAboutTopics.Count} knowsAbout, {schemaData.OfferCatalogTopics.Count} offer catalog / serviceType)."
            : "Schema.org step complete — no service topics on homepage.";

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.Schema(1, schemaData, message),
            "schema",
            schemaData);
    }

    /// <summary>
    /// Site Analyzer step 1: crawl from the homepage. Public sitemap URLs are optional seeds.
    /// Persisted inventory is 2xx fetches. Throws when zero pages were fetched.
    /// </summary>
    private async Task<SiteAnalysisStepLogEntry> RunSiteUrlsAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        var generated = await sitemapGenerator.GenerateAsync(domain, browser, ct);

        if (!TryGetOrigin(domain, out var origin))
            throw new InvalidOperationException($"Sitemap generation found no pages for {domain} — invalid site URL.");

        var validInventory = generated.Inventory
            .Where(u =>
                (string.Equals(u.SourceType, "sitemap", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(u.SourceType, "generated", StringComparison.OrdinalIgnoreCase))
                && IsSameOrigin(u.Url, origin))
            .ToList();

        if (validInventory.Count == 0)
            throw new InvalidOperationException($"Sitemap generation found no pages for {domain}.");

        var persistUrls = await profileRepo.ReplaceDiscoveredUrlsAsync(
            profileId,
            validInventory
                .GroupBy(u => $"{u.Url}␟{u.SourceType}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Select(u => new SiteAnalysisProfileDiscoveredUrlWrite(u.Url, u.SourceType))
                .ToList(),
            ct);
        if (!persistUrls.IsSuccess)
            throw new InvalidOperationException(persistUrls.Error ?? "Failed to persist discovered URLs.");

        var xmlArtifact = SitemapGenerator.BuildUrlsetXml(validInventory.Select(u => u.Url));
        var sitemapUrlCount = validInventory.Count(u => string.Equals(u.SourceType, "sitemap", StringComparison.OrdinalIgnoreCase));
        var generatedUrlCount = validInventory.Count - sitemapUrlCount;

        var sitemapData = new SitemapData(
            [],
            validInventory.Count,
            validInventory.Select(u => u.Url).ToList());

        var message =
            $"Sitemap generated: {validInventory.Count} URL(s) ({sitemapUrlCount} from public sitemap.xml, {generatedUrlCount} discovered by crawl). sitemap.xml artifact updated.";

        logger.LogInformation(
            "Site analysis step 1 (site_urls) complete for profile {ProfileId}: inventoryUrls: {InventoryUrls}",
            profileId,
            validInventory.Count);

        var entry = SiteAnalysisStepLogBuilder.SiteUrls(2, sitemapData, message);
        entry = entry with
        {
            Outputs = new Dictionary<string, object?>(entry.Outputs, StringComparer.OrdinalIgnoreCase)
            {
                ["inventoryUrls"] = validInventory.Count,
                ["sitemapXml"] = xmlArtifact,
            },
        };

        return SiteAnalyzerStepArtifactStore.WithArtifact(entry, "site_urls", sitemapData);
    }

    private static bool TryGetOrigin(string siteUrl, out string origin)
    {
        origin = string.Empty;
        try
        {
            var uri = new Uri(SiteUrlNormalizer.Normalize(siteUrl));
            origin = uri.GetLeftPart(UriPartial.Authority);
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

    private async Task<SiteAnalysisStepLogEntry> RunNavAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        var navData = browser is not null
            ? await navMenuExtractor.ExtractAsync(domain, browser, ct)
            : new NavMenuData([], "skipped");
        var persistLinks = await profileRepo.ReplaceNavigationLinksAsync(
            profileId,
            navData.Pillars.Select((pillar, index) => new SiteAnalysisProfileNavigationLinkWrite(
                domain,
                pillar.PageUrl ?? $"{domain.TrimEnd('/')}/{pillar.Slug.TrimStart('/')}",
                pillar.Name,
                navData.ExtractMethod,
                index))
                .ToList(),
            ct);
        if (!persistLinks.IsSuccess)
            throw new InvalidOperationException(persistLinks.Error ?? "Failed to persist navigation links.");
        var message = navData.ExtractMethod switch
        {
            "skipped" => "Navigation step skipped — browser unavailable.",
            _ => $"Navigation: {navData.Pillars.Count} link groups ({navData.ExtractMethod}).",
        };

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.Nav(3, navData, message),
            "nav",
            navData);
    }

    private async Task<SiteAnalysisStepLogEntry> RunHeadingsAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        var headings = await headingsExtractor.ExtractAsync(domain, browser, ct);
        var persistHeadings = await profileRepo.ReplaceHeadingsAsync(
            profileId,
            headings.Headings
                .Select((heading, index) => new SiteAnalysisProfileHeadingWrite(
                    domain,
                    heading.Level,
                    heading.Text,
                    index))
                .ToList(),
            ct);
        if (!persistHeadings.IsSuccess)
            throw new InvalidOperationException(persistHeadings.Error ?? "Failed to persist headings.");
        var message =
            headings.Headings.Count > 0 || !string.IsNullOrWhiteSpace(headings.Title)
                ? $"Headings: {headings.Headings.Count} elements from homepage."
                : "Headings: none found on homepage.";

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.Headings(4, headings, message),
            "headings",
            headings);
    }

    private async Task<SiteAnalysisStepLogEntry> RunPageContentAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        var pageContent = await pageContentExtractor.ExtractAsync(domain, browser, ct);
        var persistContent = await profileRepo.ReplacePageContentAsync(
            profileId,
            SiteAnalyzerStepRelationalLoader.ToPageContentWrite(domain, pageContent),
            ct);
        if (!persistContent.IsSuccess)
            throw new InvalidOperationException(persistContent.Error ?? "Failed to persist page content.");
        var parts = new List<string>();
        if (pageContent.VerticalTopics.Count > 0)
            parts.Add($"{pageContent.VerticalTopics.Count} H2/H3 vertical section(s)");
        if (pageContent.ServicePhrases.Count > 0)
            parts.Add($"{pageContent.ServicePhrases.Count} body phrase(s)");
        var message = parts.Count > 0
            ? $"Page content: {string.Join(", ", parts)} from homepage."
            : "Page content: no additional service phrases on homepage.";

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.PageContent(5, pageContent, message),
            "page_content",
            pageContent);
    }

    /// <summary>
    /// Unlimited same-origin crawl from homepage plus inventory seeds. Succeeds when at least
    /// one page was fetched. Unfetched inventory URLs are logged, not an abort.
    /// </summary>
    private async Task<SiteAnalysisStepLogEntry> RunSiteCrawlAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        logger.LogInformation("Site crawl starting for profile {ProfileId} domain {Domain}", profileId, domain);
        var sitemap = await SiteAnalyzerStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, [], ct);
        var inventoryUrls = sitemap.SampleUrls
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var crawlData = await sitePageCrawler.CrawlAsync(domain, inventoryUrls, browser, ct);
        var crawlUrls = crawlData.Pages.Select(p => p.Url).ToList();
        var fetchedSet = new HashSet<string>(crawlUrls, StringComparer.OrdinalIgnoreCase);
        var message = SiteCrawlInventoryCompleteness.Evaluate(inventoryUrls, fetchedSet, out var missing);
        if (missing.Count > 0)
        {
            var sample = string.Join(", ", missing.Take(10));
            var suffix = missing.Count > 10 ? $" (+{missing.Count - 10} more)" : string.Empty;
            logger.LogWarning(
                "Site crawl for profile {ProfileId}: excluding {MissingCount} inventory URL(s) not fetched: {Sample}{Suffix}",
                profileId,
                missing.Count,
                sample,
                suffix);
        }

        logger.LogInformation(
            "Site crawl extracted for profile {ProfileId}: {Message}",
            profileId,
            message);

        // Heading tree is built in memory, then stored as PageContext markdown on site pages
        // (Analyze always writes). Nested TreeJson remains for Analyzer gap detection only.
        // 404 / soft-404 chrome is not a document — do not put it in structure, gaps, or GCC pages.
        // Index-time canonical consolidation: a site serving 200 on both www and apex yields two
        // identical pages. Collapse them here so one document produces one tree row.
        var documentPages = CanonicalPageConsolidator
            .Consolidate(crawlData.Pages.Where(p => p.HasDocument).ToList());
        var documentUrls = documentPages.Select(p => p.Url).ToList();
        var treeWrites = documentPages
            .Select(page => new SiteAnalysisPageSectionTreeWrite(
                page.Url,
                JsonSerializer.Serialize(PageSectionTreeBuilder.Build(page.Html))))
            .ToList();
        var persistTrees = await profileRepo.ReplacePageSectionTreesAsync(profileId, treeWrites, ct);
        if (!persistTrees.IsSuccess)
            throw new InvalidOperationException(persistTrees.Error ?? "Failed to persist per-page section trees.");

        // Clear stale flat heading rows left by the early homepage-only step — trees replace them.
        var clearFlat = await profileRepo.ReplaceHeadingsAsync(profileId, [], ct);
        if (!clearFlat.IsSuccess)
            throw new InvalidOperationException(clearFlat.Error ?? "Failed to clear flat headings after tree persist.");

        await PersistCrawlDiscoveredUrlsAsync(profileId, documentUrls, ct);
        await PersistSiteStructureAsync(
            profileId,
            crawlData,
            SiteAnalyzerStepRelationalLoader.EmptyInternalLinks(crawlData.PagesFetched),
            SiteAnalyzerStepRelationalLoader.EmptyUrlPatterns(),
            ct);
        logger.LogInformation("Site crawl persisted for profile {ProfileId}", profileId);

        var artifact = new SiteAnalyzerStepArtifactStore.SiteStructureArtifact(
            crawlData,
            SiteAnalyzerStepRelationalLoader.EmptyInternalLinks(crawlData.PagesFetched),
            SiteAnalyzerStepRelationalLoader.EmptyUrlPatterns(),
            documentUrls);

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.SiteCrawl(6, crawlData, message),
            "site_crawl",
            artifact);
    }

    private async Task<SiteAnalysisStepLogEntry> RunInternalLinksAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var structure = await SiteAnalyzerStepRelationalLoader.LoadSiteCrawlAsync(profileRepo, profileId, [], ct);
        var internalLinks = internalLinkExtractor.Extract(structure.Crawl, domain);
        var message =
            $"Internal links: {internalLinks.Links.Count} link(s) ({internalLinks.Links.Count(l => !l.InferredFromUrlSlug)} anchor, {internalLinks.Links.Count(l => l.InferredFromUrlSlug)} from URL slug).";

        await PersistSiteStructureAsync(
            profileId,
            structure.Crawl,
            internalLinks,
            structure.UrlPatterns,
            ct);

        var artifact = structure with { InternalLinks = internalLinks };

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.InternalLinks(7, structure.Crawl, internalLinks, message),
            "internal_links",
            artifact);
    }

    private async Task<SiteAnalysisStepLogEntry> RunUrlPatternsAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var sitemap = await SiteAnalyzerStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, [], ct);
        var structure = await SiteAnalyzerStepRelationalLoader.LoadSiteStructureAsync(profileRepo, profileId, [], ct);
        var patternUrls = sitemap.SampleUrls
            .Concat(structure.CrawledUrls)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var urlPatterns = urlPatternExtractor.Extract(patternUrls, domain);
        var message = $"URL patterns: {urlPatterns.Topics.Count} topic(s) from {urlPatterns.UrlsScanned} URL(s).";

        await PersistSiteStructureAsync(
            profileId,
            structure.Crawl,
            structure.InternalLinks,
            urlPatterns,
            ct);

        var artifact = structure with { UrlPatterns = urlPatterns };

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.UrlPatterns(8, urlPatterns, message),
            "url_patterns",
            artifact);
    }

    private async Task PersistCrawlDiscoveredUrlsAsync(
        Guid profileId,
        IReadOnlyList<string> crawlUrls,
        CancellationToken ct)
    {
        var discoveredUrls = await profileRepo.GetDiscoveredUrlsAsync(profileId, ct);
        if (!discoveredUrls.IsSuccess)
            throw new InvalidOperationException(discoveredUrls.Error ?? "Failed to load discovered URL inventory.");
        var existingInventory = discoveredUrls.Value ?? [];
        var refreshedInventory = existingInventory
            .Where(x => !string.Equals(x.SourceType, "crawl", StringComparison.OrdinalIgnoreCase))
            .Select(x => new SiteAnalysisProfileDiscoveredUrlWrite(x.Url, x.SourceType, x.LastSeenAt))
            .Concat(crawlUrls
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(url => new SiteAnalysisProfileDiscoveredUrlWrite(url, "crawl")))
            .ToList();
        var persistInventory = await profileRepo.ReplaceDiscoveredUrlsAsync(profileId, refreshedInventory, ct);
        if (!persistInventory.IsSuccess)
            throw new InvalidOperationException(persistInventory.Error ?? "Failed to persist discovered URL inventory.");
    }

    private async Task PersistSiteStructureAsync(
        Guid profileId,
        SiteCrawlData crawlData,
        InternalLinkData internalLinks,
        UrlPatternData urlPatterns,
        CancellationToken ct)
    {
        var persistStructure = await profileRepo.ReplaceSiteStructureAsync(
            profileId,
            SiteAnalyzerStepRelationalLoader.ToSiteStructureWrite(
                crawlData,
                internalLinks,
                urlPatterns,
                forceDocumentWrite: true),
            ct);
        if (!persistStructure.IsSuccess)
            throw new InvalidOperationException(persistStructure.Error ?? "Failed to persist site structure.");
    }

    private Task<SiteAnalysisStepLogEntry> RunCompleteAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult(
            SiteAnalysisStepLogBuilder.Complete(9, now, now.AddDays(30), "Analysis complete!"));
    }

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
}
