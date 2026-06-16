using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Services.NicheExtraction;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services.NicheStepRunners;

public sealed class NicheStepExecutionService(
    INicheProfileRepository profileRepo,
    IProjectRepository projectRepo,
    NicheAnalysisPersistenceService persistence,
    SchemaOrgExtractor schemaExtractor,
    SitemapExtractor sitemapExtractor,
    NavMenuExtractor navMenuExtractor,
    HomepageHeadingsExtractor headingsExtractor,
    PageContentExtractor pageContentExtractor,
    SitePageCrawler sitePageCrawler,
    InternalLinkExtractor internalLinkExtractor,
    UrlPatternExtractor urlPatternExtractor,
    PillarSelector pillarSelector,
    PillarDemandEnricher pillarDemandEnricher,
    GscQueryExtractor gscQueryExtractor,
    NicheAuthorityScorer scorer,
    NicheRootEntityBuilder rootBuilder,
    ILogger<NicheStepExecutionService> logger)
{
    private const int MaxSiteCrawlPages = 20;
    private sealed record KeywordArtifact(
        IReadOnlyList<PillarKeywordEnrichment> Keywords,
        bool Skipped,
        string? SkipReason,
        string Provider);

    private sealed record SerpValidationArtifact(
        IReadOnlyList<PillarSerpEnrichment> Validations,
        IReadOnlyList<NicheCompetitor> Competitors,
        IReadOnlyList<string> DemotedSlugs,
        bool Skipped,
        string? SkipReason,
        string Provider);

    private sealed record ProfileArtifact(
        string PrimaryNiche,
        string AudienceType,
        string[] NicheTags);

    public Task<NicheAnalysisStepLogEntry> RunAsync(
        string slug,
        Guid profileId,
        Guid userId,
        string domain,
        IBrowser? browser,
        CancellationToken ct) =>
        slug switch
        {
            "schema" => RunSchemaAsync(profileId, domain, browser, ct),
            "site_urls" => RunSiteUrlsAsync(profileId, domain, ct),
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
            _ => throw new InvalidOperationException($"Unknown niche step '{slug}'."),
        };

    private async Task<NicheAnalysisStepLogEntry> RunSchemaAsync(
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

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.Schema(1, schemaData, message),
            "schema",
            schemaData);
    }

    private async Task<NicheAnalysisStepLogEntry> RunSiteUrlsAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var sitemapData = await sitemapExtractor.ExtractAsync(domain, ct);
        var persistUrls = await profileRepo.ReplaceDiscoveredUrlsAsync(
            profileId,
            sitemapData.SampleUrls
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(url => new NicheProfileDiscoveredUrlWrite(url, "sitemap"))
                .ToList(),
            ct);
        if (!persistUrls.IsSuccess)
            throw new InvalidOperationException(persistUrls.Error ?? "Failed to persist discovered URLs.");
        var message = sitemapData.TotalUrlsScanned > 0
            ? $"Site URLs: {sitemapData.TotalUrlsScanned} from sitemap."
            : "Site URLs: none found in sitemap.";

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.SiteUrls(2, sitemapData, message),
            "site_urls",
            sitemapData);
    }

    private async Task<NicheAnalysisStepLogEntry> RunNavAsync(
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
            navData.Pillars.Select((pillar, index) => new NicheProfileNavigationLinkWrite(
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

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.Nav(3, navData, message),
            "nav",
            navData);
    }

    private async Task<NicheAnalysisStepLogEntry> RunHeadingsAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        var headings = await headingsExtractor.ExtractAsync(domain, browser, ct);
        var persistHeadings = await profileRepo.ReplaceHeadingsAsync(
            profileId,
            headings.Headings
                .Select((heading, index) => new NicheProfileHeadingWrite(
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

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.Headings(4, headings, message),
            "headings",
            headings);
    }

    private async Task<NicheAnalysisStepLogEntry> RunPageContentAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        var pageContent = await pageContentExtractor.ExtractAsync(domain, browser, ct);
        var persistContent = await profileRepo.ReplacePageContentAsync(
            profileId,
            NicheStepRelationalLoader.ToPageContentWrite(domain, pageContent),
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

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.PageContent(5, pageContent, message),
            "page_content",
            pageContent);
    }

    private async Task<NicheAnalysisStepLogEntry> RunSiteCrawlAsync(
        Guid profileId,
        string domain,
        IBrowser? browser,
        CancellationToken ct)
    {
        logger.LogInformation("Site crawl starting for profile {ProfileId} domain {Domain}", profileId, domain);
        var sitemap = await NicheStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, [], ct);
        var crawlData = await sitePageCrawler.CrawlAsync(
            domain,
            sitemap.SampleUrls,
            browser,
            ct,
            maxPages: MaxSiteCrawlPages);
        var crawlUrls = crawlData.Pages.Select(p => p.Url).ToList();
        var message =
            $"Site crawl: {crawlData.PagesFetched} page(s) fetched from {crawlData.PagesAttempted} attempt(s).";
        logger.LogInformation(
            "Site crawl extracted for profile {ProfileId}: {Message}",
            profileId,
            message);

        await PersistCrawlDiscoveredUrlsAsync(profileId, crawlUrls, ct);
        await PersistSiteStructureAsync(
            profileId,
            crawlData,
            NicheStepRelationalLoader.EmptyInternalLinks(crawlData.PagesFetched),
            NicheStepRelationalLoader.EmptyUrlPatterns(),
            ct);
        logger.LogInformation("Site crawl persisted for profile {ProfileId}", profileId);

        var artifact = new NicheStepArtifactStore.SiteStructureArtifact(
            crawlData,
            NicheStepRelationalLoader.EmptyInternalLinks(crawlData.PagesFetched),
            NicheStepRelationalLoader.EmptyUrlPatterns(),
            crawlUrls);

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.SiteCrawl(6, crawlData, message),
            "site_crawl",
            artifact);
    }

    private async Task<NicheAnalysisStepLogEntry> RunInternalLinksAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var structure = await NicheStepRelationalLoader.LoadSiteCrawlAsync(profileRepo, profileId, [], ct);
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

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.InternalLinks(7, structure.Crawl, internalLinks, message),
            "internal_links",
            artifact);
    }

    private async Task<NicheAnalysisStepLogEntry> RunUrlPatternsAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var sitemap = await NicheStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, [], ct);
        var structure = await NicheStepRelationalLoader.LoadSiteStructureAsync(profileRepo, profileId, [], ct);
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

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.UrlPatterns(8, urlPatterns, message),
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
            .Select(x => new NicheProfileDiscoveredUrlWrite(x.Url, x.SourceType, x.LastSeenAt))
            .Concat(crawlUrls
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(url => new NicheProfileDiscoveredUrlWrite(url, "crawl")))
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
            NicheStepRelationalLoader.ToSiteStructureWrite(crawlData, internalLinks, urlPatterns),
            ct);
        if (!persistStructure.IsSuccess)
            throw new InvalidOperationException(persistStructure.Error ?? "Failed to persist site structure.");
    }

    private async Task<NicheAnalysisStepLogEntry> RunMergingAsync(
        Guid profileId,
        Guid userId,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var inputs = await NicheStepRelationalLoader.LoadMergingInputsAsync(
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
            inputs.Structure.UrlPatterns);
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
            new NichePhaseStatusPatch(
                StructureStatus: "pending",
                EnrichmentStatus: "pending",
                PersistStage: "candidates"),
            ct);

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.Merging(
                9,
                fused.AllCandidates.Count,
                merged.Count,
                merged,
                CountBySource(fused.AllCandidates, "schema"),
                CountBySource(fused.AllCandidates, "sitemap"),
                CountBySource(fused.AllCandidates, "nav"),
                CountBySource(fused.AllCandidates, "heading"),
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

    private async Task<NicheAnalysisStepLogEntry> RunKeywordsAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var fused = await LoadFusionAsync(profileId, ct);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var location = await GetLocationAsync(profile.ProjectId, ct);
        var keyword = await pillarDemandEnricher.EnrichKeywordsOnlyAsync(merged, location, null, ct);
        var provider = keyword.Enrichments.FirstOrDefault()?.Error is not null ? "disabled" : "keyword";
        var message = keyword.Skipped
            ? $"Keyword demand skipped — {keyword.SkipReason ?? "provider unavailable"}."
            : $"Keyword demand: enriched {keyword.Enrichments.Count(k => k.Enriched)} of {keyword.Enrichments.Count} pillar(s).";

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.Keywords(
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

    private async Task<NicheAnalysisStepLogEntry> RunSerpValidationAsync(
        Guid profileId,
        string domain,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var fused = await LoadFusionAsync(profileId, ct);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var location = await GetLocationAsync(profile.ProjectId, ct);
        var serp = await pillarDemandEnricher.ValidateSerpOnlyAsync(merged, domain, location, null, ct);
        var provider = serp.Validations.FirstOrDefault()?.Provider ?? "unknown";
        var competitors = PillarDemandEnricher.BuildCompetitors(
            profileId,
            PillarDemandEnricher.NormalizeHost(domain),
            serp.Validations);
        var pillarsAfterDemotion = PillarDemandEnricher.ApplySerpDemotions(merged, serp.Validations, out var demotedSlugs);
        var updatedFusion = ApplySerpDemotionsToFusion(fused, pillarsAfterDemotion, demotedSlugs);

        await TrySaveFusionSnapshotAsync(profileId, updatedFusion, "serp_validation", ct);

        await profileRepo.UpdatePhaseStatusAsync(
            profileId,
            new NichePhaseStatusPatch(EnrichmentStatus: serp.Skipped ? "skipped" : "complete"),
            ct);

        var message = serp.Skipped
            ? $"SERP validation skipped — {serp.SkipReason ?? "provider unavailable"}."
            : demotedSlugs.Count > 0
                ? $"SERP validation: {serp.Validations.Count} pillar(s) checked, {demotedSlugs.Count} demoted, {competitors.Count} competitor(s) found."
                : $"SERP validation: {serp.Validations.Count} pillar(s) checked, {competitors.Count} competitor(s) found.";

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.SerpValidation(
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
                    provider),
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

    private async Task<NicheAnalysisStepLogEntry> RunProfileAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var artifact = await BuildProfileArtifactAsync(profileId, profile.Domain, steps, ct);
        var message = $"Niche profile: {artifact.PrimaryNiche}.";

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.Profile(
                12,
                artifact.PrimaryNiche,
                artifact.AudienceType,
                artifact.NicheTags,
                message),
            "profile",
            artifact);
    }

    private async Task<NicheAnalysisStepLogEntry> RunLocalAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId, ct);
        var profile = await LoadProfileAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var schema = await NicheStepRelationalLoader.LoadSchemaAsync(profileRepo, profileId, steps, ct);
        var sitemap = await NicheStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, steps, ct);
        var structure = await NicheStepRelationalLoader.LoadSiteStructureAsync(profileRepo, profileId, steps, ct);
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
            ? NicheAnalysisStepLogBuilder.Local(13, localGeo, message)
            : NicheAnalysisStepLogBuilder.LocalDisabled(13, message);

        return NicheStepArtifactStore.WithArtifact(entry, "local", localGeo);
    }

    private async Task<NicheAnalysisStepLogEntry> RunCoverageAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var sitemap = await NicheStepRelationalLoader.LoadSitemapAsync(profileRepo, profileId, steps, ct);
        var structure = await NicheStepRelationalLoader.LoadSiteStructureAsync(profileRepo, profileId, steps, ct);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var keywordArtifact = NicheStepArtifactStore.TryGetArtifact<KeywordArtifact>(steps, "keywords", "keywords");
        var serpArtifact = NicheStepArtifactStore.TryGetArtifact<SerpValidationArtifact>(steps, "serp_validation", "serp_validation");
        var profile = await LoadProfileAsync(profileId, ct);

        var existingPillars = profile.Pillars.ToDictionary(p => p.PillarSlug, StringComparer.OrdinalIgnoreCase);
        var existingSubtopics = profile.Pillars
            .SelectMany(p => p.Subtopics)
            .ToDictionary(
                s => $"{s.PillarId:N}:{s.TargetKeyword}",
                StringComparer.OrdinalIgnoreCase);

        var pillars = BuildNichePillars(
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

        var subtopics = BuildSubtopics(pillars, merged, existingSubtopics);
        AttachSubtopics(pillars, subtopics);

        var coverageResult = NicheContentCoverageMatcher.Apply(
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

        return NicheStepArtifactStore.WithArtifact(
            NicheAnalysisStepLogBuilder.Coverage(
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

    private async Task<NicheAnalysisStepLogEntry> RunScoringAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var profile = await LoadProfileAsync(profileId, ct);
        var fused = await LoadFusionAsync(profileId, ct);
        var steps = await LoadStepLogAsync(profileId, ct);
        var structure = await NicheStepRelationalLoader.TryLoadSiteStructureAsync(
            profileRepo,
            profileId,
            steps,
            ct);
        var serpArtifact = NicheStepArtifactStore.TryGetArtifact<SerpValidationArtifact>(
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
            new NicheProfileSummaryPatch(
                profileArtifact.PrimaryNiche,
                profile.NicheDescription,
                profileArtifact.NicheTags,
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

        return NicheAnalysisStepLogBuilder.Scoring(
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

    private Task<NicheAnalysisStepLogEntry> RunCompleteAsync(
        Guid profileId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult(
            NicheAnalysisStepLogBuilder.Complete(16, now, now.AddDays(30), "Analysis complete!"));
    }

    private async Task<ProfileArtifact> ResolveProfileArtifactAsync(
        Guid profileId,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var fromLog = NicheStepArtifactStore.TryGetArtifact<ProfileArtifact>(steps, "profile", "profile");
        if (fromLog is not null)
            return fromLog;

        var profile = await LoadProfileAsync(profileId, ct);
        if (!string.IsNullOrWhiteSpace(profile.PrimaryNiche))
        {
            return new ProfileArtifact(
                profile.PrimaryNiche,
                string.IsNullOrWhiteSpace(profile.AudienceType) ? "local_service" : profile.AudienceType,
                profile.NicheTags is { Length: > 0 } ? profile.NicheTags : []);
        }

        return await BuildProfileArtifactAsync(profileId, profile.Domain, steps, ct);
    }

    private async Task<ProfileArtifact> BuildProfileArtifactAsync(
        Guid profileId,
        string domain,
        IReadOnlyList<NicheAnalysisStepLogEntry> steps,
        CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId, ct);
        var schema = await NicheStepRelationalLoader.LoadSchemaAsync(profileRepo, profileId, steps, ct);
        var headings = await NicheStepRelationalLoader.LoadHeadingsAsync(
            profileRepo,
            profileId,
            domain,
            steps,
            ct);
        var pillars = fused.SelectedPillars
            .Select(PillarSelector.ToDiscoveredPillar)
            .Select((p, index) => new NichePillar
            {
                Id = Guid.NewGuid(),
                NicheProfileId = profileId,
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
        var nicheTags = BuildNicheTags(schema, pillars).ToArray();
        return new ProfileArtifact(rootEntity, audienceType, nicheTags);
    }

    private async Task<NicheProfile> LoadProfileAsync(Guid profileId, CancellationToken ct)
    {
        var result = await profileRepo.GetByIdAsync(profileId, ct);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException("Profile not found.");
        return result.Value;
    }

    private async Task<IReadOnlyList<NicheAnalysisStepLogEntry>> LoadStepLogAsync(Guid profileId, CancellationToken ct)
    {
        var result = await profileRepo.GetAnalysisDetailsRowAsync(profileId, includeFusion: false, ct);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException("Step log not available.");
        return NicheStepArtifactStore.ParseSteps(result.Value.AnalysisStepLog);
    }

    private async Task<SiteTopicProfile> LoadFusionAsync(Guid profileId, CancellationToken ct)
    {
        var result = await profileRepo.GetAnalysisDetailsRowAsync(profileId, includeFusion: true, ct);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException("Fusion snapshot not found — run merging first.");

        var steps = NicheAnalysisStepLogJson.Parse(result.Value.AnalysisStepLog);
        var fusion = await NicheStepRunState.LoadMergedFusionSnapshotAsync(
            profileRepo,
            profileId,
            result.Value.FusionSnapshot,
            steps,
            ct);
        return fusion ?? throw new InvalidOperationException("Fusion snapshot is malformed.");
    }

    private async Task<string> GetLocationAsync(Guid projectId, CancellationToken ct)
    {
        var projectResult = await projectRepo.GetByIdAsync(projectId, ct);
        return projectResult.IsSuccess && !string.IsNullOrWhiteSpace(projectResult.Value?.DefaultLocation)
            ? projectResult.Value.DefaultLocation
            : "United States";
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

    private static List<NichePillar> BuildNichePillars(
        IReadOnlyList<DiscoveredPillar> merged,
        Guid profileId,
        IReadOnlyList<PillarKeywordEnrichment> keywordMetrics,
        IReadOnlyList<PillarSerpEnrichment> serpValidations,
        IReadOnlyDictionary<string, NichePillar> existingBySlug)
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

            return new NichePillar
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                NicheProfileId = profileId,
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

    private static List<NicheSubtopic> BuildSubtopics(
        List<NichePillar> pillars,
        IReadOnlyList<DiscoveredPillar> discovered,
        IReadOnlyDictionary<string, NicheSubtopic> existingByKey)
    {
        var subtopics = new List<NicheSubtopic>();
        var discMap = discovered.ToDictionary(d => d.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var pillar in pillars)
        {
            if (!discMap.TryGetValue(pillar.PillarSlug, out var disc))
                continue;

            var childSlugs = disc.ChildSlugs.Take(10).ToList();
            foreach (var childSlug in childSlugs)
            {
                var keyword = $"{pillar.PrimaryKeyword} {childSlug.Replace('-', ' ')}".Trim();
                var key = $"{pillar.Id:N}:{keyword}";
                existingByKey.TryGetValue(key, out var existing);

                subtopics.Add(new NicheSubtopic
                {
                    Id = existing?.Id ?? Guid.NewGuid(),
                    PillarId = pillar.Id,
                    SubtopicTitle = SitemapExtractor.SlugToTitle(childSlug),
                    TargetKeyword = keyword,
                    SearchIntent = pillar.SearchIntent == "local" ? "local" : "informational",
                    CoverageStatus = existing?.CoverageStatus ?? "gap",
                    ExistingUrl = existing?.ExistingUrl,
                    RecommendedFormat = existing?.RecommendedFormat ?? InferFormat(childSlug),
                    FixEffort = existing?.FixEffort ?? "create",
                    SearchVolume = existing?.SearchVolume ?? 0,
                    KeywordDifficulty = existing?.KeywordDifficulty ?? 0m,
                    RecommendedWordCount = existing?.RecommendedWordCount ?? 0,
                    IsQuickWin = existing?.IsQuickWin ?? false,
                });
            }

            if (childSlugs.Count < 3)
            {
                var generic = new[]
                {
                    ("what-is", "informational", "definition"),
                    ("how-much-does-cost", "commercial", "how_to"),
                    ("near-me", "local", "local_page"),
                    ("how-to", "informational", "how_to"),
                    ("benefits", "informational", "listicle"),
                };

                foreach (var (suffix, intent, format) in generic)
                {
                    var keyword = $"{pillar.PrimaryKeyword} {suffix.Replace('-', ' ')}".Trim();
                    var key = $"{pillar.Id:N}:{keyword}";
                    existingByKey.TryGetValue(key, out var existing);

                    subtopics.Add(new NicheSubtopic
                    {
                        Id = existing?.Id ?? Guid.NewGuid(),
                        PillarId = pillar.Id,
                        SubtopicTitle = $"{pillar.PillarTopic} – {SitemapExtractor.SlugToTitle(suffix)}",
                        TargetKeyword = keyword,
                        SearchIntent = existing?.SearchIntent ?? intent,
                        CoverageStatus = existing?.CoverageStatus ?? "gap",
                        ExistingUrl = existing?.ExistingUrl,
                        RecommendedFormat = existing?.RecommendedFormat ?? format,
                        FixEffort = existing?.FixEffort ?? "create",
                        SearchVolume = existing?.SearchVolume ?? 0,
                        KeywordDifficulty = existing?.KeywordDifficulty ?? 0m,
                        RecommendedWordCount = existing?.RecommendedWordCount ?? 0,
                        IsQuickWin = existing?.IsQuickWin ?? false,
                    });
                }
            }
        }

        return subtopics;
    }

    private static void AttachSubtopics(List<NichePillar> pillars, List<NicheSubtopic> subtopics)
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

    private static string InferFormat(string slug)
    {
        if (slug.Contains("how") || slug.Contains("guide")) return "how_to";
        if (slug.Contains("cost") || slug.Contains("price") || slug.Contains("cheap")) return "comparison";
        if (slug.Contains("best") || slug.Contains("top")) return "listicle";
        if (slug.Contains("near") || slug.Contains("location")) return "local_page";
        if (slug.Contains("vs") || slug.Contains("compare")) return "comparison";
        if (slug.Contains("faq") || slug.Contains("question")) return "faq";
        return "how_to";
    }

    private static string DetermineAudienceType(List<NichePillar> pillars, SchemaOrgData schema)
    {
        var hasLocalPillars = pillars.Any(p => p.SearchIntent == "local");
        var hasLocationArea = schema.AreaServed.Count > 0;
        if (hasLocalPillars || hasLocationArea) return "local_service";

        var hasInfoPillars = pillars.Count(p => p.SearchIntent == "informational");
        if (hasInfoPillars > pillars.Count / 2) return "blog";

        return "local_service";
    }

    private static IEnumerable<string> BuildNicheTags(SchemaOrgData schema, List<NichePillar> pillars)
    {
        var tags = new List<string>();
        tags.AddRange(schema.AreaServed.Take(3));
        tags.AddRange(pillars.Take(3).Select(p => p.PillarTopic));
        return tags.Distinct(StringComparer.OrdinalIgnoreCase).Take(8);
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

    private static List<NicheProfileSchemaSignalWrite> BuildSchemaSignals(SchemaOrgData schemaData)
    {
        var signals = new List<NicheProfileSchemaSignalWrite>();
        var order = 0;

        signals.AddRange(schemaData.ServiceNames.Select(value =>
            new NicheProfileSchemaSignalWrite("service", "name", value, null, order++)));
        signals.AddRange(schemaData.KnowsAboutTopics.Select(value =>
            new NicheProfileSchemaSignalWrite("thing", "knowsAbout", value, null, order++)));
        signals.AddRange(schemaData.OfferCatalogTopics.Select(value =>
            new NicheProfileSchemaSignalWrite("offer_catalog", "serviceType", value, null, order++)));
        signals.AddRange(schemaData.AreaServed.Select(value =>
            new NicheProfileSchemaSignalWrite("organization", "areaServed", value, null, order++)));
        signals.AddRange(schemaData.SameAsUrls.Select(value =>
            new NicheProfileSchemaSignalWrite("organization", "sameAs", value, value, order++)));

        if (!string.IsNullOrWhiteSpace(schemaData.Description))
            signals.Add(new NicheProfileSchemaSignalWrite("organization", "description", schemaData.Description, null, order++));
        if (!string.IsNullOrWhiteSpace(schemaData.BrandName))
            signals.Add(new NicheProfileSchemaSignalWrite("organization", "brandName", schemaData.BrandName, null, order++));

        return signals;
    }

    private static string[] SampleExclusionReasons(SiteTopicProfile fused) =>
        fused.ExclusionReasons
            .Take(20)
            .Select(kvp => $"{kvp.Key}: {kvp.Value}")
            .ToArray();
}
