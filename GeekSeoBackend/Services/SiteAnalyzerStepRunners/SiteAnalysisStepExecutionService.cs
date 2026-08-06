using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services.SiteExtraction;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.SiteAnalyzerStepRunners;

public sealed class SiteAnalyzerStepExecutionService(
    ISiteAnalysisProfileRepository profileRepo,
    SiteAnalysisPersistenceService persistence,
    SchemaOrgExtractor schemaExtractor,
    SitemapGenerator sitemapGenerator,
    NavMenuExtractor navMenuExtractor,
    HomepageHeadingsExtractor headingsExtractor,
    PageContentExtractor pageContentExtractor,
    SitePageCrawler sitePageCrawler,
    InternalLinkExtractor internalLinkExtractor,
    UrlPatternExtractor urlPatternExtractor,
    PillarSelector pillarSelector,
    PillarDemandEnricher pillarDemandEnricher,
    ILocalSerpContextResolver localSerpContextResolver,
    GscQueryExtractor gscQueryExtractor,
    SiteAuthorityScorer scorer,
    SiteRootEntityBuilder rootBuilder,
    ILogger<SiteAnalyzerStepExecutionService> logger)
{
    private sealed record KeywordArtifact(
        IReadOnlyList<PillarKeywordEnrichment> Keywords,
        bool Skipped,
        string? SkipReason,
        string Provider);

    private sealed record SerpValidationArtifact(
        IReadOnlyList<PillarSerpEnrichment> Validations,
        IReadOnlyList<SiteAnalysisCompetitor> Competitors,
        IReadOnlyList<string> DemotedSlugs,
        bool Skipped,
        string? SkipReason,
        string Provider);

    private sealed record ProfileArtifact(
        string PrimaryFocus,
        string AudienceType,
        string[] FocusTags);

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
            "merging" => RunMergingAsync(profileId, userId, ct),
            "keywords" => RunKeywordsAsync(profileId, domain, ct),
            "serp_validation" => RunSerpValidationAsync(profileId, domain, ct),
            "profile" => RunProfileAsync(profileId, ct),
            "local" => RunLocalAsync(profileId, ct),
            "coverage" => RunCoverageAsync(profileId, ct),
            "scoring" => RunScoringAsync(profileId, ct),
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
    /// Site Analyzer step 1 (sitemap generation): always regenerates the full URL inventory via
    /// <see cref="SitemapGenerator"/> — unlimited same-origin BFS crawl merged with any public
    /// <c>/sitemap.xml</c> URLs — and rewrites the persisted <c>sitemap.xml</c> artifact from that
    /// inventory on every Analyze. Fails closed: zero discovered URLs, or persisted rows outside
    /// <c>SourceType ∈ {{sitemap, generated}}</c> / not same-origin, throw rather than soft-continue.
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
    /// Unlimited, inventory-complete site crawl: fetches every URL in the step-1 inventory (no
    /// page cap, no attempt-budget soft-stop). Success requires every inventory URL to have been
    /// fetched — not <c>PagesFetched >= 1</c> — otherwise this throws with the missing URLs
    /// listed so the failure is diagnosable rather than a soft "N pages fetched" success.
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
        var missing = inventoryUrls.Where(u => !fetchedSet.Contains(u)).ToList();

        if (missing.Count > 0)
        {
            var sample = string.Join(", ", missing.Take(10));
            var suffix = missing.Count > 10 ? $" (+{missing.Count - 10} more)" : string.Empty;
            throw new InvalidOperationException(
                $"Site crawl incomplete: {missing.Count} of {inventoryUrls.Count} inventory URL(s) were not fetched: {sample}{suffix}");
        }

        var message =
            $"Site crawl: {crawlData.PagesFetched} of {inventoryUrls.Count} inventory page(s) fetched (complete).";
        logger.LogInformation(
            "Site crawl extracted for profile {ProfileId}: {Message}",
            profileId,
            message);

        // Real per-page heading+paragraph tree (h1-h6, content-backed) — single source of truth.
        // Flat SiteAnalysisProfileHeading rows are cleared below; LoadHeadingsAsync derives a
        // flat list from these trees for the candidate pool. Must run here while raw HTML is
        // still in scope: PersistSiteStructureAsync strips it to VisibleText only.
        var treeWrites = crawlData.Pages
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

        await PersistCrawlDiscoveredUrlsAsync(profileId, crawlUrls, ct);
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
            crawlUrls);

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
            SiteAnalyzerStepRelationalLoader.ToSiteStructureWrite(crawlData, internalLinks, urlPatterns),
            ct);
        if (!persistStructure.IsSuccess)
            throw new InvalidOperationException(persistStructure.Error ?? "Failed to persist site structure.");
    }

    private async Task<SiteAnalysisStepLogEntry> RunMergingAsync(
        Guid profileId,
        Guid userId,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var inputs = await SiteAnalyzerStepRelationalLoader.LoadMergingInputsAsync(
            profileRepo,
            profileId,
            profile.Domain,
            steps,
            ct);

        var gscOverlay = await gscQueryExtractor.ExtractAsync(userId, profile.ProjectId, ct);
        var candidatePool = TopicCandidatePoolBuilder.Build(
            inputs.Schema,
            inputs.Sitemap,
            inputs.Nav,
            inputs.Headings,
            inputs.PageContent,
            inputs.Structure.InternalLinks,
            inputs.Structure.UrlPatterns,
            inputs.ContentBackedHeadingSlugs);
        candidatePool = GscQueryExtractor.ApplyToPool(candidatePool, gscOverlay);
        var fused = pillarSelector.Select(candidatePool, inputs.Schema.AreaServed.ToList());
        fused = NormalizedTopicalityCalculator.Apply(fused, inputs.Structure.Crawl, inputs.Structure.UrlPatterns);
        var mergeResult = pillarSelector.ToPillarMergeResult(fused);
        var merged = mergeResult.Selected;
        var silentGscSlugs = GscQueryExtractor.FindSilentPillarSlugs(merged, gscOverlay);
        var gscMatchedCount = CountBySource(fused.AllCandidates, "gsc");
        var mergeMessage = BuildMergeMessage(mergeResult, fused, gscOverlay, gscMatchedCount, silentGscSlugs);

        var candidatePersist = await persistence.PersistCandidatesAsync(profileId, fused, includeEvidence: true, ct);
        if (!candidatePersist.IsSuccess)
            throw new InvalidOperationException(candidatePersist.Error ?? "Failed to persist topic candidates.");

        await TrySaveFusionSnapshotAsync(profileId, fused, "merging", ct);

        await profileRepo.UpdatePhaseStatusAsync(
            profileId,
            new SiteAnalysisPhaseStatusPatch(
                StructureStatus: "pending",
                EnrichmentStatus: "pending",
                PersistStage: "candidates"),
            ct);

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.Merging(
                9,
                fused.AllCandidates.Count,
                merged.Count,
                merged,
                CountBySource(fused.AllCandidates, "schema"),
                CountBySource(fused.AllCandidates, "sitemap"),
                CountBySource(fused.AllCandidates, "nav"),
                CountBySource(fused.AllCandidates, "heading") + CountBySource(fused.AllCandidates, "heading_content_backed"),
                mergeResult.Excluded,
                CountBySource(fused.AllCandidates, "page"),
                CountBySource(fused.AllCandidates, "page_vertical"),
                CountBySource(fused.AllCandidates, "internal_link"),
                CountBySource(fused.AllCandidates, "url_pattern"),
                CountBySource(fused.AllCandidates, "same_as"),
                CountBySource(fused.AllCandidates, "gsc"),
                fused.SulVersion,
                fused.SignalSourcesPresent,
                SampleExclusionReasons(fused),
                gscOverlay.Connected,
                gscOverlay.Skipped,
                gscOverlay.SkipReason,
                gscOverlay.QueryRowCount,
                gscMatchedCount,
                silentGscSlugs,
                fused.NormalizedTopicalityBySlug,
                mergeMessage),
            "merging",
            fused);
    }

    private async Task<SiteAnalysisStepLogEntry> RunKeywordsAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var fused = await LoadFusionAsync(profileId, ct);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var localContext = await ResolveLocalSerpContextAsync(profile.ProjectId, ct);
        var keyword = await pillarDemandEnricher.EnrichKeywordsOnlyAsync(
            merged, localContext.SerpMarketLocation, null, ct);
        var provider = keyword.Enrichments.FirstOrDefault()?.Error is not null ? "disabled" : "keyword";
        var message = keyword.Skipped
            ? $"Keyword demand skipped — {keyword.SkipReason ?? "provider unavailable"}."
            : $"Keyword demand: enriched {keyword.Enrichments.Count(k => k.Enriched)} of {keyword.Enrichments.Count} pillar(s).";

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.Keywords(
                10,
                new PillarDemandEnrichment(
                    keyword.Enrichments,
                    [],
                    [],
                    merged,
                    [],
                    keyword.Skipped,
                    true,
                    keyword.SkipReason,
                    "SERP validation not run in step 11.",
                    provider,
                    "disabled"),
                message),
            "keywords",
            new KeywordArtifact(keyword.Enrichments, keyword.Skipped, keyword.SkipReason, provider));
    }

    private async Task<SiteAnalysisStepLogEntry> RunSerpValidationAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var fused = await LoadFusionAsync(profileId, ct);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var localContext = await ResolveLocalSerpContextAsync(profile.ProjectId, ct);
        var siteBusiness = await LoadSiteBusinessProfileAsync(profileId, domain, ct);
        var serp = await pillarDemandEnricher.ValidateSerpOnlyAsync(
            merged,
            domain,
            localContext.SerpMarketLocation,
            localContext.ServiceArea,
            null,
            ct);
        var provider = serp.Validations.FirstOrDefault()?.Provider ?? "unknown";
        var (competitors, filteredAdjacent) = PillarDemandEnricher.BuildCompetitors(
            profileId,
            PillarDemandEnricher.NormalizeHost(domain),
            serp.Validations,
            merged,
            siteBusiness);
        var pillarsAfterDemotion = PillarDemandEnricher.ApplySerpDemotions(merged, serp.Validations, out var demotedSlugs);
        var updatedFusion = ApplySerpDemotionsToFusion(fused, pillarsAfterDemotion, demotedSlugs);

        await TrySaveFusionSnapshotAsync(profileId, updatedFusion, "serp_validation", ct);

        await profileRepo.UpdatePhaseStatusAsync(
            profileId,
            new SiteAnalysisPhaseStatusPatch(EnrichmentStatus: serp.Skipped ? "skipped" : "complete"),
            ct);

        if (!serp.Skipped && competitors.Count > 0)
        {
            var saveCompetitors = await profileRepo.BulkInsertCompetitorsAsync(competitors, ct);
            if (!saveCompetitors.IsSuccess)
            {
                logger.LogWarning(
                    "Competitors not saved during serp_validation for {ProfileId}: {Error}",
                    profileId,
                    saveCompetitors.Error);
            }
        }

        var (message, localWarning) = SerpValidationMessages.Build(
            serp.Validations,
            competitors,
            serp.Skipped,
            serp.SkipReason,
            serp.LocalStats,
            demotedSlugs.Count);
        if (localWarning is not null)
        {
            logger.LogWarning(
                "SERP validation local warning for profile {ProfileId}: {Warning}",
                profileId,
                localWarning);
        }

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.SerpValidation(
                11,
                new PillarDemandEnrichment(
                    [],
                    serp.Validations,
                    competitors,
                    pillarsAfterDemotion,
                    demotedSlugs,
                    true,
                    serp.Skipped,
                    "Keyword demand not run in step 10.",
                    serp.SkipReason,
                    "disabled",
                    provider,
                    serp.LocalStats,
                    filteredAdjacent),
                message),
            "serp_validation",
            new SerpValidationArtifact(
                serp.Validations,
                competitors,
                demotedSlugs,
                serp.Skipped,
                serp.SkipReason,
                provider));
    }

    private async Task<SiteAnalysisStepLogEntry> RunProfileAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var artifact = await BuildProfileArtifactAsync(profileId, profile.Domain, steps, ct);
        var message = $"site analysis profile: {artifact.PrimaryFocus}.";

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.Profile(
                12,
                artifact.PrimaryFocus,
                artifact.AudienceType,
                artifact.FocusTags,
                message),
            "profile",
            artifact);
    }

    private async Task<SiteAnalysisStepLogEntry> RunLocalAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId, ct);
        var profile = await LoadProfileAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var schema = await SiteAnalyzerStepRelationalLoader.LoadSchemaAsync(profileRepo, profileId, steps, ct);
        var sitemap = await SiteAnalyzerStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, steps, ct);
        var structure = await SiteAnalyzerStepRelationalLoader.LoadSiteStructureAsync(profileRepo, profileId, steps, ct);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();

        var localGeo = LocalGapGenerator.Analyze(
            schema,
            sitemap,
            structure.CrawledUrls,
            structure.UrlPatterns,
            merged);
        var updatedFusion = fused with { LocalGeography = localGeo };

        await TrySaveFusionSnapshotAsync(profileId, updatedFusion, "local", ct);

        var message = !localGeo.IsLocalBusiness
            ? "Local geography: no areaServed or location URLs detected."
            : localGeo.Gaps.Count > 0
                ? $"Local geography: {localGeo.AreasServed.Count} area(s) declared, {localGeo.LocationPagesFound.Count} location page(s), {localGeo.Gaps.Count} gap(s)."
                : localGeo.AreasServed.Count > 0
                    ? $"Local geography: {localGeo.AreasServed.Count} area(s) declared — all have matching location pages."
                    : $"Local geography: {localGeo.LocationPagesFound.Count} location page(s) found.";

        var entry = localGeo.IsLocalBusiness
            ? SiteAnalysisStepLogBuilder.Local(13, localGeo, message)
            : SiteAnalysisStepLogBuilder.LocalDisabled(13, message);

        return SiteAnalyzerStepArtifactStore.WithArtifact(entry, "local", localGeo);
    }

    private async Task<SiteAnalysisStepLogEntry> RunCoverageAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var sitemap = await SiteAnalyzerStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, steps, ct);
        var structure = await SiteAnalyzerStepRelationalLoader.LoadSiteStructureAsync(profileRepo, profileId, steps, ct);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var keywordArtifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<KeywordArtifact>(steps, "keywords", "keywords");
        var serpArtifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<SerpValidationArtifact>(steps, "serp_validation", "serp_validation");
        var profile = await LoadProfileAsync(profileId, ct);

        var existingPillars = profile.Pillars.ToDictionary(p => p.PillarSlug, StringComparer.OrdinalIgnoreCase);
        var existingSubtopics = profile.Pillars
            .SelectMany(p => p.Subtopics)
            .ToDictionary(
                s => $"{s.PillarId:N}:{s.TargetKeyword}",
                StringComparer.OrdinalIgnoreCase);

        var pillars = BuildSiteAnalysisPillars(
            merged,
            profileId,
            keywordArtifact?.Keywords ?? [],
            serpArtifact?.Validations ?? [],
            existingPillars);
        foreach (var pillar in pillars)
        {
            if (pillar.Id == Guid.Empty)
                pillar.Id = Guid.NewGuid();
        }

        var pageTrees = await SiteAnalyzerStepRelationalLoader.LoadPageSectionTreesAsync(
            profileRepo, profileId, ct);
        var allUrls = structure.Crawl.Pages
            .Select(p => p.Url)
            .Concat(sitemap.SampleUrls)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var subtopics = BuildSubtopics(pillars, existingSubtopics, pageTrees, allUrls);
        AttachSubtopics(pillars, subtopics);

        var coverageResult = SiteContentCoverageMatcher.Apply(
            pillars,
            subtopics,
            fused,
            merged,
            structure.Crawl,
            sitemap,
            serpArtifact?.Validations ?? []);

        scorer.ScorePillars(pillars);

        var savePillars = await profileRepo.BulkInsertPillarsAsync(pillars, ct);
        if (!savePillars.IsSuccess)
            throw new InvalidOperationException(savePillars.Error ?? "Failed to persist coverage pillars.");

        var saveSubtopics = await profileRepo.BulkInsertSubtopicsAsync(subtopics, ct);
        if (!saveSubtopics.IsSuccess)
        {
            logger.LogWarning(
                "Subtopics not saved for {ProfileId}: {Error}",
                profileId,
                saveSubtopics.Error);
        }

        var message =
            $"Content coverage: {coverageResult.PillarsCovered} covered, {coverageResult.PillarsPartial} partial, {coverageResult.PillarsGap} gap — {coverageResult.SubtopicsCovered}/{coverageResult.SubtopicsTotal} subtopics matched to URLs.";

        return SiteAnalyzerStepArtifactStore.WithArtifact(
            SiteAnalysisStepLogBuilder.Coverage(
                14,
                coverageResult.PillarsCovered,
                coverageResult.PillarsPartial,
                coverageResult.PillarsGap,
                coverageResult.SubtopicsCovered,
                coverageResult.SubtopicsTotal,
                coverageResult.SamplePartialPillars,
                message),
            "coverage",
            coverageResult);
    }

    private async Task<SiteAnalysisStepLogEntry> RunScoringAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var fused = await LoadFusionAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var structure = await SiteAnalyzerStepRelationalLoader.TryLoadSiteStructureAsync(
            profileRepo,
            profileId,
            steps,
            ct);
        var serpArtifact = SiteAnalyzerStepArtifactStore.TryGetArtifact<SerpValidationArtifact>(
            steps,
            "serp_validation",
            "serp_validation");
        var profileArtifact = await ResolveProfileArtifactAsync(profileId, steps, ct);

        var pillars = profile.Pillars.OrderBy(p => p.DisplayOrder).ToList();
        var authorityScore = scorer.ComputeTopicalAuthorityScore(pillars);
        var covered = pillars.Count(p => p.CoverageStatus == "covered");
        var partial = pillars.Count(p => p.CoverageStatus == "partial");
        var gap = pillars.Count(p => p.CoverageStatus == "gap");
        var analyzedAt = DateTimeOffset.UtcNow;

        var enrichedFusion = structure is null
            ? fused
            : TopicSnapshotEnricher.Apply(
                fused,
                structure.InternalLinks,
                structure.UrlPatterns,
                serpArtifact?.Validations ?? []);

        var saveResult = await persistence.SaveCompletionAsync(
            profileId,
            new SiteAnalysisProfileSummaryPatch(
                profileArtifact.PrimaryFocus,
                profile.FocusDescription,
                profileArtifact.FocusTags,
                profileArtifact.AudienceType,
                pillars.Count,
                analyzedAt,
                analyzedAt.AddDays(30),
                ScanFingerprint: null,
                ScanChangeScore: null,
                PersistStage: "scores",
                StructureStatus: "complete",
                EnrichmentStatus: serpArtifact?.Skipped == true ? "skipped" : "complete"),
            authorityScore,
            covered,
            partial,
            gap,
            enrichedFusion,
            writeFusionArchive: true,
            ct);
        if (!saveResult.IsSuccess)
            throw new InvalidOperationException(saveResult.Error ?? "Failed to persist scoring summary.");

        if (serpArtifact is { Competitors.Count: > 0 })
        {
            var saveCompetitors = await profileRepo.BulkInsertCompetitorsAsync(serpArtifact.Competitors, ct);
            if (!saveCompetitors.IsSuccess)
            {
                logger.LogWarning(
                    "Competitors not saved for {ProfileId}: {Error}",
                    profileId,
                    saveCompetitors.Error);
            }
        }

        var scoringMessage = $"Authority score: {authorityScore:F0}/100 — results saved.";
        var entityThinCount = enrichedFusion.EntityCoverageBySlug.Values.Count(c => c.IsEntityThin);
        var linkGraphEdgeCount = enrichedFusion.InternalLinkGraph?.Edges.Count ?? 0;
        var orphanPillarCount = enrichedFusion.InternalLinkGraph?.OrphanSlugs.Count ?? 0;

        return SiteAnalysisStepLogBuilder.Scoring(
            15,
            authorityScore,
            covered,
            partial,
            gap,
            pillars.Count,
            scoringMessage,
            entityThinCount,
            linkGraphEdgeCount,
            orphanPillarCount,
            enrichedFusion.RecommendedActions.Count);
    }

    private Task<SiteAnalysisStepLogEntry> RunCompleteAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult(
            SiteAnalysisStepLogBuilder.Complete(16, now, now.AddDays(30), "Analysis complete!"));
    }

    private async Task<ProfileArtifact> ResolveProfileArtifactAsync(
        Guid profileId,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var fromLog = SiteAnalyzerStepArtifactStore.TryGetArtifact<ProfileArtifact>(steps, "profile", "profile");
        if (fromLog is not null)
            return fromLog;

        var profile = await LoadProfileAsync(profileId, ct);
        if (!string.IsNullOrWhiteSpace(profile.PrimaryFocus))
        {
            return new ProfileArtifact(
                profile.PrimaryFocus,
                string.IsNullOrWhiteSpace(profile.AudienceType) ? "local_service" : profile.AudienceType,
                profile.FocusTags is { Length: > 0 } ? profile.FocusTags : []);
        }

        return await BuildProfileArtifactAsync(profileId, profile.Domain, steps, ct);
    }

    private async Task<ProfileArtifact> BuildProfileArtifactAsync(
        Guid profileId,
        string domain,
        IReadOnlyList<SiteAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId, ct);
        var schema = await SiteAnalyzerStepRelationalLoader.LoadSchemaAsync(profileRepo, profileId, steps, ct);
        var headings = await SiteAnalyzerStepRelationalLoader.LoadHeadingsAsync(
            profileRepo,
            profileId,
            domain,
            steps,
            ct);
        var pillars = fused.SelectedPillars
            .Select(PillarSelector.ToDiscoveredPillar)
            .Select((p, index) => new SiteAnalysisPillar
            {
                Id = Guid.NewGuid(),
                SiteAnalysisProfileId = profileId,
                PillarTopic = p.Name,
                PillarSlug = p.Slug,
                PrimaryKeyword = p.Name.ToLowerInvariant(),
                SearchIntent = p.Intent,
                Source = p.Source,
                DisplayOrder = index,
            })
            .ToList();

        var rootEntity = rootBuilder.Build(schema, headings, pillars);
        var audienceType = DetermineAudienceType(pillars, schema);
        var focusTags = BuildFocusTags(schema, pillars).ToArray();
        return new ProfileArtifact(rootEntity, audienceType, focusTags);
    }

    private async Task<SiteAnalysisProfile> LoadProfileAsync(Guid profileId, CancellationToken ct)
    {
        var result = await profileRepo.GetByIdAsync(profileId, ct);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException("Profile not found.");
        return result.Value;
    }

    private async Task<IReadOnlyList<SiteAnalysisStepLogEntry>> LoadStepLogAsync(Guid profileId, CancellationToken ct)
    {
        var result = await profileRepo.GetAnalysisDetailsRowAsync(profileId, includeFusion: false, ct);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException("Step log not available.");
        return SiteAnalyzerStepArtifactStore.ParseSteps(result.Value.AnalysisStepLog);
    }

    private async Task<SiteTopicProfile> LoadFusionAsync(Guid profileId, CancellationToken ct)
    {
        var result = await profileRepo.GetAnalysisDetailsRowAsync(profileId, includeFusion: true, ct);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException("Fusion snapshot not found — run merging first.");

        var steps = SiteAnalysisStepLogJson.Parse(result.Value.AnalysisStepLog);
        var fusion = await SiteAnalyzerStepRunState.LoadMergedFusionSnapshotAsync(
            profileRepo,
            profileId,
            result.Value.FusionSnapshot,
            steps,
            ct);
        return fusion ?? throw new InvalidOperationException("Fusion snapshot is malformed.");
    }

    private async Task<SiteBusinessProfile> LoadSiteBusinessProfileAsync(
        Guid profileId,
        string domain,
        CancellationToken ct) =>
        await SiteAnalyzerStepRelationalLoader.LoadSiteBusinessProfileAsync(profileRepo, profileId, domain, ct);

    private async Task<LocalSerpContext> ResolveLocalSerpContextAsync(Guid projectId, CancellationToken ct)
    {
        var resolved = await localSerpContextResolver.ResolveAsync(projectId, ct);
        if (resolved.IsSuccess && resolved.Value is not null)
            return resolved.Value;

        logger.LogWarning(
            "Could not resolve local SERP context for project {ProjectId}: {Error}",
            projectId,
            resolved.Error);
        return new LocalSerpContext(PillarDemandEnricher.NationalLocation, null);
    }

    private async Task TrySaveFusionSnapshotAsync(
        Guid profileId,
        SiteTopicProfile fused,
        string stepSlug,
        CancellationToken ct)
    {
        var saveFusion = await profileRepo.SaveFusionSnapshotAsync(
            profileId,
            SiteTopicProfileJson.SerializeForPersistence(fused),
            ct);

        if (saveFusion.IsSuccess)
            return;

        if (IsRouteUnavailable(saveFusion.Error))
        {
            logger.LogWarning(
                "Fusion snapshot persistence unavailable during {StepSlug} for {ProfileId}; continuing with step log artifacts",
                stepSlug,
                profileId);
            return;
        }

        throw new InvalidOperationException(saveFusion.Error ?? $"Failed to persist fusion snapshot during {stepSlug}.");
    }

    private static bool IsRouteUnavailable(string? error) =>
        error is not null
        && (error.Contains("404", StringComparison.OrdinalIgnoreCase)
            || error.Contains("NotFound", StringComparison.OrdinalIgnoreCase));

    private static SiteTopicProfile ApplySerpDemotionsToFusion(
        SiteTopicProfile fused,
        IReadOnlyList<DiscoveredPillar> selected,
        IReadOnlyList<string> demotedSlugs)
    {
        var kept = selected.Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var demoted = fused.SelectedPillars
            .Where(p => !kept.Contains(p.Slug))
            .ToList();
        var reasons = new Dictionary<string, string>(fused.ExclusionReasons, StringComparer.OrdinalIgnoreCase);
        foreach (var slug in demotedSlugs)
            reasons[slug] = "Demoted by SERP validation.";

        var excluded = fused.ExcludedCandidates
            .Concat(demoted)
            .GroupBy(c => c.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        return fused with
        {
            SelectedPillars = fused.SelectedPillars.Where(p => kept.Contains(p.Slug)).ToList(),
            ExcludedCandidates = excluded,
            ExclusionReasons = reasons,
        };
    }

    private static List<SiteAnalysisPillar> BuildSiteAnalysisPillars(
        IReadOnlyList<DiscoveredPillar> merged,
        Guid profileId,
        IReadOnlyList<PillarKeywordEnrichment> keywordMetrics,
        IReadOnlyList<PillarSerpEnrichment> serpValidations,
        IReadOnlyDictionary<string, SiteAnalysisPillar> existingBySlug)
    {
        var metricsBySlug = keywordMetrics
            .Where(k => k.Enriched)
            .ToDictionary(k => k.Slug, StringComparer.OrdinalIgnoreCase);
        var serpBySlug = serpValidations
            .ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);

        static string ToJson<T>(IReadOnlyList<T>? list) =>
            list is { Count: > 0 } ? System.Text.Json.JsonSerializer.Serialize(list) : "[]";

        return merged.Select((p, idx) =>
        {
            metricsBySlug.TryGetValue(p.Slug, out var metrics);
            serpBySlug.TryGetValue(p.Slug, out var serp);
            existingBySlug.TryGetValue(p.Slug, out var existing);

            return new SiteAnalysisPillar
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                SiteAnalysisProfileId = profileId,
                PillarTopic = p.Name,
                PillarSlug = p.Slug,
                PrimaryKeyword = metrics?.Keyword ?? p.Name.ToLowerInvariant(),
                PageUrl = p.PageUrl,
                SearchIntent = p.Intent,
                Source = p.Source,
                DisplayOrder = idx,
                CoverageStatus = existing?.CoverageStatus ?? "gap",
                CoverageScore = existing?.CoverageScore ?? 0m,
                ExistingPageCount = existing?.ExistingPageCount ?? 0,
                RequiredSubtopicCount = Math.Max(p.ChildPageCount, 5),
                CoveredSubtopicCount = existing?.CoveredSubtopicCount ?? 0,
                SearchVolume = metrics?.SearchVolume ?? existing?.SearchVolume ?? 0,
                KeywordDifficulty = metrics?.KeywordDifficulty ?? existing?.KeywordDifficulty ?? 0m,
                StrategicPriority = existing?.StrategicPriority ?? "expansion",
                Priority = existing?.Priority ?? 0,
                ContentAngle = existing?.ContentAngle,
                EstimatedTrafficPotential = existing?.EstimatedTrafficPotential ?? 0m,
                PaaQuestionsJson = ToJson(serp?.PaaQuestions),
                RelatedSearchesJson = ToJson(serp?.RelatedSearches),
                LocalPaaQuestionsJson = ToJson(serp?.LocalPaaQuestions),
                LocalRelatedSearchesJson = ToJson(serp?.LocalRelatedSearches),
            };
        }).ToList();
    }

    /// <summary>
    /// Gap subtopics from the real per-page <see cref="PageSection"/> tree only — never from
    /// sitemap URL-segment childSlugs (those manufactured a gap for every path segment). A node
    /// is a gap when it sits on a page belonging to the pillar (or is nested under a matching
    /// pillar heading anywhere) and has no dedicated page. Bare headings count.
    /// </summary>
    private static List<SiteAnalysisSubtopic> BuildSubtopics(
        List<SiteAnalysisPillar> pillars,
        IReadOnlyDictionary<string, SiteAnalysisSubtopic> existingByKey,
        IReadOnlyList<(string PageUrl, IReadOnlyList<PageSection> Tree)> pageTrees,
        IReadOnlyList<string> allUrls)
    {
        var subtopics = new List<SiteAnalysisSubtopic>();

        foreach (var pillar in pillars)
        {
            foreach (var (headingText, headingSlug) in SiteContentCoverageMatcher.CollectTreeGaps(
                         pillar.PillarSlug, pageTrees, allUrls))
            {
                var keyword = $"{pillar.PrimaryKeyword} {headingText}".Trim();
                var key = $"{pillar.Id:N}:{keyword}";
                existingByKey.TryGetValue(key, out var existing);

                subtopics.Add(new SiteAnalysisSubtopic
                {
                    Id = existing?.Id ?? Guid.NewGuid(),
                    PillarId = pillar.Id,
                    SubtopicTitle = headingText,
                    TargetKeyword = keyword,
                    SearchIntent = existing?.SearchIntent
                        ?? (pillar.SearchIntent == "local" ? "local" : "informational"),
                    CoverageStatus = existing?.CoverageStatus ?? "gap",
                    ExistingUrl = existing?.ExistingUrl,
                    RecommendedFormat = existing?.RecommendedFormat ?? InferFormat(headingSlug),
                    FixEffort = existing?.FixEffort ?? "create",
                    SearchVolume = existing?.SearchVolume ?? 0,
                    KeywordDifficulty = existing?.KeywordDifficulty ?? 0m,
                    RecommendedWordCount = existing?.RecommendedWordCount ?? 0,
                    IsQuickWin = existing?.IsQuickWin ?? false,
                });
            }
        }

        return subtopics;
    }

    private static void AttachSubtopics(List<SiteAnalysisPillar> pillars, List<SiteAnalysisSubtopic> subtopics)
    {
        var byPillar = subtopics
            .GroupBy(s => s.PillarId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var pillar in pillars)
        {
            pillar.Subtopics = byPillar.TryGetValue(pillar.Id, out var list)
                ? list
                : [];
        }
    }

    /// <summary>
    /// Recommends a content format only when the slug gives a real signal for one. Returns ""
    /// (no recommendation) rather than a guessed default — asserting e.g. "how_to" for slugs with
    /// no matching keyword is a fake-confident label, not a real recommendation.
    /// </summary>
    private static string InferFormat(string slug)
    {
        if (slug.Contains("how") || slug.Contains("guide")) return "how_to";
        if (slug.Contains("cost") || slug.Contains("price") || slug.Contains("cheap")) return "comparison";
        if (slug.Contains("best") || slug.Contains("top")) return "listicle";
        if (slug.Contains("near") || slug.Contains("location")) return "local_page";
        if (slug.Contains("vs") || slug.Contains("compare")) return "comparison";
        if (slug.Contains("faq") || slug.Contains("question")) return "faq";
        return "";
    }

    private static string DetermineAudienceType(List<SiteAnalysisPillar> pillars, SchemaOrgData schema)
    {
        var hasLocalPillars = pillars.Any(p => p.SearchIntent == "local");
        var hasLocationArea = schema.AreaServed.Count > 0;
        if (hasLocalPillars || hasLocationArea) return "local_service";

        var hasInfoPillars = pillars.Count(p => p.SearchIntent == "informational");
        if (hasInfoPillars > pillars.Count / 2) return "blog";

        return "local_service";
    }

    private static IEnumerable<string> BuildFocusTags(SchemaOrgData schema, List<SiteAnalysisPillar> pillars)
    {
        var tags = new List<string>();
        tags.AddRange(schema.AreaServed);
        tags.AddRange(pillars.Select(p => p.PillarTopic));
        return tags.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildMergeMessage(
        PillarMergeResult mergeResult,
        SiteTopicProfile fused,
        GscOwnerOverlay gscOverlay,
        int gscMatchedCount,
        IReadOnlyList<string> silentGscSlugs)
    {
        var baseMessage = mergeResult.Excluded.Count > 0
            ? $"Topic pillars: {mergeResult.Selected.Count} selected, {mergeResult.Excluded.Count} excluded by fusion gates. Fused {fused.AllCandidates.Count} peer candidate(s) ({string.Join(", ", fused.SignalSourcesPresent)})."
            : $"Topic pillars: {mergeResult.Selected.Count} after fusion of {fused.AllCandidates.Count} peer candidate(s) ({string.Join(", ", fused.SignalSourcesPresent)}).";

        if (!gscOverlay.Connected)
            return $"{baseMessage} GSC not connected — owner query overlay skipped.";

        if (gscOverlay.Skipped)
            return $"{baseMessage} GSC overlay skipped — {gscOverlay.SkipReason ?? "unavailable"}.";

        var gscPart = gscMatchedCount > 0
            ? $"GSC: {gscOverlay.QueryRowCount} query rows, {gscMatchedCount} pillar(s) confirmed."
            : $"GSC: {gscOverlay.QueryRowCount} query rows, no pillar matches yet.";

        if (silentGscSlugs.Count > 0)
            gscPart += $" {silentGscSlugs.Count} selected pillar(s) have no matching GSC cluster.";

        return $"{baseMessage} {gscPart}";
    }

    private static int CountBySource(IReadOnlyList<TopicCandidate> pool, string source) =>
        pool.Count(c => c.Evidence.Any(e => e.Source.Equals(source, StringComparison.OrdinalIgnoreCase)));

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

    private static string[] SampleExclusionReasons(SiteTopicProfile fused) =>
        fused.ExclusionReasons
            .Select(kvp => $"{kvp.Key}: {kvp.Value}")
            .ToArray();
}
