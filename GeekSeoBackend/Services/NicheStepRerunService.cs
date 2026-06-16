using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Services.NicheExtraction;
using GeekSeoBackend.Services.NicheStepRunners;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services;

/// <summary>
/// Runs individual analysis steps in isolation. Each step reads its required
/// inputs from the DB (fusion snapshot, crawled URLs, pillar rows) rather than
/// in-memory state from prior steps.
/// </summary>
public sealed class NicheStepRerunService(
    INicheProfileRepository profileRepo,
    INicheAnalyticsDapperRepository analyticsRepo,
    SchemaOrgExtractor schemaExtractor,
    SitemapExtractor sitemapExtractor,
    NavMenuExtractor navMenuExtractor,
    HomepageHeadingsExtractor headingsExtractor,
    PageContentExtractor pageContentExtractor,
    SitePageCrawler sitePageCrawler,
    InternalLinkExtractor internalLinkExtractor,
    UrlPatternExtractor urlPatternExtractor,
    PillarSelector pillarSelector,
    GscQueryExtractor gscQueryExtractor,
    PillarDemandEnricher pillarDemandEnricher,
    NicheAnalysisPersistenceService persistence,
    NicheAuthorityScorer scorer,
    NicheRootEntityBuilder rootBuilder,
    NicheStepExecutionService stepExecution,
    NicheStepLock stepLock,
    NicheAnalysisProgressNotifier progressNotifier,
    IServiceScopeFactory scopeFactory,
    WorkerUserContext workerUser,
    ILogger<NicheStepRerunService> logger)
{
    private static int TotalSteps => NicheStepCatalog.Ordered.Count;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Re-runs a single step identified by <paramref name="slug"/>.
    /// Validates dependencies, acquires lock, cascades invalidation, executes step.
    /// </summary>
    public async Task<(bool Success, string? Error)> RerunStepAsync(
        Guid profileId, Guid userId, string slug, IBrowser? browser, CancellationToken ct)
    {
        if (!NicheStepCatalog.BySlug.TryGetValue(slug, out var definition))
            return (false, $"Unknown step slug '{slug}'.");

        SemaphoreSlim? sem = null;
        var lockHeld = false;
        try
        {
            var statusResult = await profileRepo.GetStepStatusesAsync(profileId, ct);
            if (!statusResult.IsSuccess)
                return (false, $"Could not load step statuses: {statusResult.Error}");
            var statuses = statusResult.Value ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dep in definition.Dependencies)
            {
                if (!IsDependencyComplete(statuses, dep))
                    return await FailRerunAsync(
                        profileId, userId, definition,
                        $"Dependency '{dep}' must be complete before running '{slug}'.", ct);
            }

            sem = stepLock.Get(profileId);
            if (!await sem.WaitAsync(0, ct))
                return await FailRerunAsync(
                    profileId, userId, definition,
                    "Another step is already running for this profile.", ct);
            lockHeld = true;

            var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profileResult.IsSuccess || profileResult.Value is null)
                return await FailRerunAsync(profileId, userId, definition, "Profile not found.", ct);
            var profileStatus = profileResult.Value.Status;

            var downstream = NicheStepCatalog.GetDownstream(slug);
            if (downstream.Count > 0)
                await profileRepo.InvalidateDownstreamStepsAsync(profileId, downstream, ct);

            if (profileStatus is "pending" or "complete" or "failed")
            {
                await profileRepo.UpdateStatusAsync(
                    profileId,
                    "processing",
                    slug,
                    definition.StepNumber,
                    TotalSteps,
                    ct: ct);
            }

            await profileRepo.UpdateStepStatusAsync(profileId, slug, "running", ct: ct);
            await NicheStepRunStatusWriter.SyncAsync(
                profileRepo, logger, profileId, slug, "running", definition, ct: ct);
            await PushStepEvent(profileId, userId, slug, definition, "running", $"Running step: {slug}…", ct);

            var entry = await ExecuteStepAsync(profileId, userId, slug, browser, ct);
            var slimEntry = NicheStepArtifactStore.ForStepLogPersistence(entry);

            // SignalR + relational step-run first — legacy step-log PATCHes can take minutes on bloated profiles.
            await PushStepEvent(profileId, userId, slug, definition, "complete", entry.Summary, ct);
            await NicheStepRunStatusWriter.SyncAsync(
                profileRepo, logger, profileId, slug, "complete", definition, slimEntry, ct: ct);
            ScheduleStepCompletionPersist(profileId, userId, slug, definition, entry, profileStatus);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Step re-run failed for profile {ProfileId} slug {Slug}", profileId, slug);
            await RecordStepFailureAsync(profileId, userId, slug, definition, ex.Message, ct);
            return (false, ex.Message);
        }
        finally
        {
            if (lockHeld && sem is not null)
                sem.Release();
        }
    }

    /// <summary>
    /// Persists step/profile error state when background work fails. Never throws — logs secondary failures.
    /// </summary>
    public async Task RecordStepFailureAsync(
        Guid profileId,
        Guid userId,
        string slug,
        NicheStepDefinition? definition,
        string message,
        CancellationToken ct)
    {
        try
        {
            NicheStepCatalog.BySlug.TryGetValue(slug, out var stepDef);
            definition ??= stepDef;
            var errorEntry = new NicheAnalysisStepLogEntry(
                definition?.StepNumber ?? 0,
                slug,
                definition?.Title ?? slug,
                "error",
                message,
                new Dictionary<string, object?>());
            await profileRepo.UpdateStepStatusAsync(profileId, slug, "error", errorEntry, ct: ct);
            await NicheStepRunStatusWriter.SyncAsync(
                profileRepo, logger, profileId, slug, "error", definition, errorEntry, message, ct);
            await profileRepo.UpdateStatusAsync(
                profileId,
                "failed",
                slug,
                definition?.StepNumber ?? 0,
                TotalSteps,
                errorMessage: message,
                stepLogEntry: errorEntry,
                ct: ct);
            await PushStepEvent(profileId, userId, slug, definition, "error", $"Step '{slug}' failed: {message}", ct);
        }
        catch (Exception persistEx)
        {
            logger.LogError(
                persistEx,
                "Could not persist step failure for profile {ProfileId} slug {Slug}: {Message}",
                profileId,
                slug,
                message);
        }
    }

    private async Task<NicheAnalysisStepLogEntry> ExecuteStepAsync(
        Guid profileId, Guid userId, string slug, IBrowser? browser, CancellationToken ct)
    {
        var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
        if (!profileResult.IsSuccess || profileResult.Value is null)
            throw new InvalidOperationException("Profile not found.");
        var profile = profileResult.Value;
        var domain = NicheSiteUrlNormalizer.Normalize(profile.Domain);
        return await stepExecution.RunAsync(slug, profileId, userId, domain, browser, ct);
    }

    // ── Steps 1–6: Extraction steps — re-execute extractors directly ──────

    private async Task<NicheAnalysisStepLogEntry> RerunSchemaAsync(
        Guid profileId, string domain, IBrowser? browser, CancellationToken ct)
    {
        var data = await schemaExtractor.ExtractAsync(domain, browser, ct);
        var msg = data.ServiceNames.Count > 0
            ? $"Found {data.ServiceNames.Count} schema topic(s)."
            : "No service topics in schema.";
        return NicheAnalysisStepLogBuilder.Schema(1, data, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunSiteUrlsAsync(
        Guid profileId, string domain, CancellationToken ct)
    {
        var data = await sitemapExtractor.ExtractAsync(domain, ct);
        var msg = data.TotalUrlsScanned > 0
            ? $"Site URLs: {data.TotalUrlsScanned} from sitemap."
            : "No URLs in sitemap.";
        return NicheAnalysisStepLogBuilder.SiteUrls(2, data, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunNavAsync(
        Guid profileId, string domain, IBrowser? browser, CancellationToken ct)
    {
        var data = browser is not null
            ? await navMenuExtractor.ExtractAsync(domain, browser, ct)
            : new NavMenuData([], "skipped");
        var msg = $"Navigation: {data.Pillars.Count} link groups ({data.ExtractMethod}).";
        return NicheAnalysisStepLogBuilder.Nav(3, data, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunHeadingsAsync(
        Guid profileId, string domain, IBrowser? browser, CancellationToken ct)
    {
        var data = await headingsExtractor.ExtractAsync(domain, browser, ct);
        var msg = $"Headings: {data.Headings.Count} elements from homepage.";
        return NicheAnalysisStepLogBuilder.Headings(4, data, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunPageContentAsync(
        Guid profileId, string domain, IBrowser? browser, CancellationToken ct)
    {
        // Always homepage only — see plan constraint
        var data = await pageContentExtractor.ExtractAsync(domain, browser, ct);
        var persistContent = await profileRepo.ReplacePageContentAsync(
            profileId,
            NicheStepRelationalLoader.ToPageContentWrite(domain, data),
            ct);
        if (!persistContent.IsSuccess)
            throw new InvalidOperationException(persistContent.Error ?? "Failed to persist page content.");
        var msg = $"Page content: {data.ServicePhrases.Count} phrase(s), {data.VerticalTopics.Count} section(s).";
        return NicheAnalysisStepLogBuilder.PageContent(5, data, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunSiteStructureAsync(
        Guid profileId, string domain, IBrowser? browser, CancellationToken ct)
    {
        var sitemap = await sitemapExtractor.ExtractAsync(domain, ct);
        var crawlData = await sitePageCrawler.CrawlAsync(domain, sitemap.SampleUrls, browser, ct);
        var internalLinks = internalLinkExtractor.Extract(crawlData, domain);
        var crawlUrls = crawlData.Pages.Select(p => p.Url).ToList();
        var patternUrls = sitemap.SampleUrls.Concat(crawlUrls).Distinct().ToList();
        var urlPatterns = urlPatternExtractor.Extract(patternUrls, domain);

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
        var replaceUrls = await profileRepo.ReplaceDiscoveredUrlsAsync(profileId, refreshedInventory, ct);
        if (!replaceUrls.IsSuccess)
            throw new InvalidOperationException(replaceUrls.Error ?? "Failed to persist discovered URL inventory.");

        var persistStructure = await profileRepo.ReplaceSiteStructureAsync(
            profileId,
            NicheStepRelationalLoader.ToSiteStructureWrite(crawlData, internalLinks, urlPatterns),
            ct);
        if (!persistStructure.IsSuccess)
            throw new InvalidOperationException(persistStructure.Error ?? "Failed to persist site structure.");

        var msg = $"Site structure: {crawlData.PagesFetched} page(s) crawled, {internalLinks.Links.Count} link(s).";
        return NicheAnalysisStepLogBuilder.SiteStructure(6, crawlData, internalLinks, urlPatterns, msg);
    }

    // ── Step 7: Merging — re-runs steps 1–6 inline then fuses ────────────

    private async Task<NicheAnalysisStepLogEntry> RerunMergingAsync(
        Guid profileId, Guid userId, NicheProfile profile, string domain, IBrowser? browser, CancellationToken ct)
    {
        // Re-extract all inputs (steps 1–6 are fast enough)
        var schema    = await schemaExtractor.ExtractAsync(domain, browser, ct);
        var sitemap   = await sitemapExtractor.ExtractAsync(domain, ct);
        var nav       = browser is not null
            ? await navMenuExtractor.ExtractAsync(domain, browser, ct)
            : new NavMenuData([], "skipped");
        var headings  = await headingsExtractor.ExtractAsync(domain, browser, ct);
        var content   = await pageContentExtractor.ExtractAsync(domain, browser, ct);
        var crawlData = await sitePageCrawler.CrawlAsync(domain, sitemap.SampleUrls, browser, ct);
        var links     = internalLinkExtractor.Extract(crawlData, domain);
        var crawlUrls = crawlData.Pages.Select(p => p.Url).ToList();
        var patterns  = urlPatternExtractor.Extract(
            sitemap.SampleUrls.Concat(crawlUrls).Distinct().ToList(), domain);

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
        var replaceUrls = await profileRepo.ReplaceDiscoveredUrlsAsync(profileId, refreshedInventory, ct);
        if (!replaceUrls.IsSuccess)
            throw new InvalidOperationException(replaceUrls.Error ?? "Failed to persist discovered URL inventory.");

        var gsc = await gscQueryExtractor.ExtractAsync(userId, profile.ProjectId, ct);
        var pool = TopicCandidatePoolBuilder.Build(schema, sitemap, nav, headings, content, links, patterns);
        pool = GscQueryExtractor.ApplyToPool(pool, gsc);
        var fused = pillarSelector.Select(pool, schema.AreaServed.ToList());
        fused = NormalizedTopicalityCalculator.Apply(fused, crawlData, patterns);
        var mergeResult = pillarSelector.ToPillarMergeResult(fused);
        var merged = mergeResult.Selected;
        var silent = GscQueryExtractor.FindSilentPillarSlugs(merged, gsc);
        var gscCount = pool.Count(c => c.Evidence.Any(e => e.Source == "gsc"));

        var persistCandidates = await persistence.PersistCandidatesAsync(profileId, fused, includeEvidence: true, ct);
        if (!persistCandidates.IsSuccess)
            throw new InvalidOperationException(persistCandidates.Error ?? "Failed to persist relational topic candidates.");
        await profileRepo.SaveFusionSnapshotAsync(profileId,
            SiteTopicProfileJson.SerializeForPersistence(fused), ct);

        var msg = $"Topic selection: {merged.Count} pillar(s) from {pool.Count} candidate(s).";
        return NicheAnalysisStepLogBuilder.Merging(7, pool.Count, merged.Count, merged,
            pool.Count(c => c.Evidence.Any(e => e.Source == "schema")),
            pool.Count(c => c.Evidence.Any(e => e.Source == "sitemap")),
            pool.Count(c => c.Evidence.Any(e => e.Source == "nav")),
            pool.Count(c => c.Evidence.Any(e => e.Source == "heading")),
            mergeResult.Excluded,
            pool.Count(c => c.Evidence.Any(e => e.Source == "page")),
            pool.Count(c => c.Evidence.Any(e => e.Source == "page_vertical")),
            pool.Count(c => c.Evidence.Any(e => e.Source == "internal_link")),
            pool.Count(c => c.Evidence.Any(e => e.Source == "url_pattern")),
            pool.Count(c => c.Evidence.Any(e => e.Source == "same_as")),
            gscCount, fused.SulVersion, fused.SignalSourcesPresent, [],
            gsc.Connected, gsc.Skipped, gsc.SkipReason, gsc.QueryRowCount, gscCount, silent,
            fused.NormalizedTopicalityBySlug, msg);
    }

    // ── Steps 8–9: Demand validation ──────────────────────────────────────

    private async Task<NicheAnalysisStepLogEntry> RerunKeywordsAsync(
        Guid profileId, NicheProfile profile, string domain, CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var location = await GetLocationAsync(profile.ProjectId, ct);
        var demand = await pillarDemandEnricher.EnrichAsync(merged, profileId, domain, location, null, ct);
        var msg = demand.KeywordsSkipped
            ? $"Keywords skipped: {demand.KeywordSkipReason}"
            : $"Keywords: {demand.Keywords.Count(k => k.Enriched)} pillar(s) enriched.";
        return NicheAnalysisStepLogBuilder.Keywords(8, demand, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunSerpValidationAsync(
        Guid profileId, NicheProfile profile, string domain, CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var location = await GetLocationAsync(profile.ProjectId, ct);
        var demand = await pillarDemandEnricher.EnrichAsync(merged, profileId, domain, location, null, ct);
        await profileRepo.BulkInsertCompetitorsAsync(demand.Competitors, ct);
        var msg = demand.SerpSkipped
            ? $"SERP skipped: {demand.SerpSkipReason}"
            : $"SERP: {demand.SerpValidations.Count} pillar(s), {demand.Competitors.Count} competitor(s).";
        return NicheAnalysisStepLogBuilder.SerpValidation(9, demand, msg);
    }

    // ── Steps 10–14: Synthesis steps — read from stored data ──────────────

    private async Task<NicheAnalysisStepLogEntry> RerunProfileAsync(
        Guid profileId, string domain, CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
        var nicheProfile = profileResult.Value!;
        var schemaData = await schemaExtractor.ExtractAsync(domain, null, ct);
        var pillars = BuildNichePillarsLight(merged, profileId);
        var rootEntity = rootBuilder.Build(schemaData, new HomepageHeadings(), pillars);
        var audience = "local_service";
        var tags = BuildNicheTagsLight(schemaData, pillars);
        var msg = $"Niche profile: {rootEntity}.";
        return NicheAnalysisStepLogBuilder.Profile(10, rootEntity, audience, tags, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunLocalAsync(
        Guid profileId, string domain, CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId);
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();
        var crawlUrls = await LoadCrawledUrlsAsync(profileId, ct);
        var schema = await schemaExtractor.ExtractAsync(domain, null, ct);
        var sitemap = await sitemapExtractor.ExtractAsync(domain, ct);
        var urlPatterns = urlPatternExtractor.Extract(crawlUrls, domain);
        var localGeo = LocalGapGenerator.Analyze(schema, sitemap, crawlUrls, urlPatterns, merged);
        fused = fused with { LocalGeography = localGeo };
        await profileRepo.SaveFusionSnapshotAsync(profileId,
            SiteTopicProfileJson.SerializeForPersistence(fused), ct);
        var msg = localGeo.IsLocalBusiness
            ? $"Local geography: {localGeo.AreasServed.Count} area(s), {localGeo.Gaps.Count} gap(s)."
            : "Local geography: not a local business.";
        return localGeo.IsLocalBusiness
            ? NicheAnalysisStepLogBuilder.Local(11, localGeo, msg)
            : NicheAnalysisStepLogBuilder.LocalDisabled(11, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunCoverageAsync(
        Guid profileId, string domain, CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId);
        var crawlUrls = await LoadCrawledUrlsAsync(profileId, ct);

        // Load stored pillars and subtopics
        var profileFull = await profileRepo.GetByIdAsync(profileId, ct);
        var pillars = profileFull.Value!.Pillars.ToList();
        var subtopics = pillars.SelectMany(p => p.Subtopics).ToList();
        var merged = fused.SelectedPillars.Select(PillarSelector.ToDiscoveredPillar).ToList();

        // Build lightweight crawl data from stored URL list (no HTML needed for coverage URL matching)
        var sitemap = await sitemapExtractor.ExtractAsync(domain, ct);
        var fakeCrawl = new SiteCrawlData(
            crawlUrls.Select(u => new CrawledPage(u, string.Empty)).ToList(),
            crawlUrls.Count, crawlUrls.Count);
        IReadOnlyList<PillarSerpEnrichment> serpValidations = [];

        var coverage = NicheContentCoverageMatcher.Apply(
            pillars, subtopics, fused, merged, fakeCrawl, sitemap, serpValidations);

        scorer.ScorePillars(pillars);
        await profileRepo.BulkInsertPillarsAsync(pillars, ct);
        await profileRepo.BulkInsertSubtopicsAsync(subtopics, ct);

        var msg = $"Coverage: {coverage.PillarsCovered} covered, {coverage.PillarsPartial} partial, {coverage.PillarsGap} gap.";
        return NicheAnalysisStepLogBuilder.Coverage(12,
            coverage.PillarsCovered, coverage.PillarsPartial, coverage.PillarsGap,
            coverage.SubtopicsCovered, coverage.SubtopicsTotal, coverage.SamplePartialPillars, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunScoringAsync(Guid profileId, CancellationToken ct)
    {
        var fused = await LoadFusionAsync(profileId);
        var profileFull = await profileRepo.GetByIdAsync(profileId, ct);
        var pillars = profileFull.Value!.Pillars.ToList();
        var score = scorer.ComputeTopicalAuthorityScore(pillars);
        var covered = pillars.Count(p => p.CoverageStatus == "covered");
        var partial = pillars.Count(p => p.CoverageStatus == "partial");
        var gap = pillars.Count(p => p.CoverageStatus == "gap");
        fused = TopicSnapshotEnricher.Apply(fused,
            new InternalLinkData([], new Dictionary<string, int>(), 0),
            new UrlPatternData([], 0), []);
        var analyzedAt = DateTimeOffset.UtcNow;
        await persistence.SaveCompletionAsync(profileId,
            new NicheProfileSummaryPatch(
                profileFull.Value.PrimaryNiche, profileFull.Value.NicheDescription,
                profileFull.Value.NicheTags, profileFull.Value.AudienceType,
                pillars.Count, analyzedAt, analyzedAt.AddDays(30),
                null, null, "scores", "complete", null),
            score, covered, partial, gap, fused, false, ct);
        var msg = $"Authority score: {score:F0}/100.";
        return NicheAnalysisStepLogBuilder.Scoring(13, score, covered, partial, gap,
            pillars.Count, msg);
    }

    private async Task<NicheAnalysisStepLogEntry> RerunCompleteAsync(Guid profileId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        await profileRepo.UpdatePhaseStatusAsync(profileId,
            new NichePhaseStatusPatch(StructureStatus: "complete", EnrichmentStatus: "complete",
                PersistStage: "done"), ct);
        await profileRepo.UpdateStatusAsync(profileId, "complete", "complete", 14, 14, ct: ct);
        return NicheAnalysisStepLogBuilder.Complete(14, now, now.AddDays(30), "Analysis complete!");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<SiteTopicProfile> LoadFusionAsync(Guid profileId)
    {
        var details = await profileRepo.GetAnalysisDetailsRowAsync(profileId, includeFusion: true);
        if (!details.IsSuccess || details.Value is null)
            throw new InvalidOperationException("Fusion snapshot not found — run merging step first.");

        var steps = NicheAnalysisStepLogJson.Parse(details.Value.AnalysisStepLog);
        var fusion = await NicheStepRunState.LoadMergedFusionSnapshotAsync(
            profileRepo,
            profileId,
            details.Value.FusionSnapshot,
            steps,
            CancellationToken.None);
        return fusion ?? throw new InvalidOperationException("Could not reconstruct topic snapshot.");
    }

    private async Task<List<string>> LoadCrawledUrlsAsync(Guid profileId, CancellationToken ct)
    {
        var urls = await profileRepo.GetDiscoveredUrlsAsync(profileId, ct);
        if (!urls.IsSuccess)
            return [];

        return (urls.Value ?? [])
            .Where(x => string.Equals(x.SourceType, "crawl", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<string> GetLocationAsync(Guid projectId, CancellationToken ct)
    {
        // Try to read from a project repo if available; default to United States
        return "United States";
    }

    private static List<NichePillar> BuildNichePillarsLight(
        IReadOnlyList<DiscoveredPillar> merged, Guid profileId) =>
        merged.Select((p, i) => new NichePillar
        {
            NicheProfileId = profileId,
            PillarTopic = p.Name,
            PillarSlug = p.Slug,
            PrimaryKeyword = p.Name.ToLowerInvariant(),
            Source = p.Source,
            DisplayOrder = i,
        }).ToList();

    private static string[] BuildNicheTagsLight(SchemaOrgData schema, List<NichePillar> pillars) =>
        schema.AreaServed.Take(3).Concat(pillars.Take(3).Select(p => p.PillarTopic)).ToArray();

    private void ScheduleStepCompletionPersist(
        Guid profileId,
        Guid userId,
        string slug,
        NicheStepDefinition definition,
        NicheAnalysisStepLogEntry entry,
        string profileStatusBeforeRun)
    {
        var slimEntry = NicheStepArtifactStore.ForStepLogPersistence(entry);
        var overallStatus = ResolveOverallStatusAfterStep(slug, profileStatusBeforeRun, definition);

        _ = Task.Run(async () =>
        {
            workerUser.UserId = userId;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<INicheProfileRepository>();
                var persistLogger = scope.ServiceProvider.GetRequiredService<ILogger<NicheStepRerunService>>();

                await repo.UpdateStepStatusAsync(profileId, slug, "complete", slimEntry, CancellationToken.None);
                await repo.UpdateStatusAsync(
                    profileId,
                    overallStatus,
                    slug,
                    definition.StepNumber,
                    TotalSteps,
                    stepLogEntry: slimEntry,
                    ct: CancellationToken.None);

                if (slug == "complete")
                {
                    await repo.UpdatePhaseStatusAsync(
                        profileId,
                        new NichePhaseStatusPatch(
                            StructureStatus: "complete",
                            EnrichmentStatus: "complete",
                            PersistStage: "done",
                            Status: "complete"),
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Background step completion persist failed for profile {ProfileId} slug {Slug}",
                    profileId,
                    slug);
            }
            finally
            {
                workerUser.UserId = Guid.Empty;
            }
        });
    }

    private static string ResolveOverallStatusAfterStep(
        string slug,
        string profileStatusBeforeRun,
        NicheStepDefinition definition)
    {
        if (string.Equals(slug, "complete", StringComparison.OrdinalIgnoreCase))
            return "complete";

        // Re-running a single step on a finished analysis should not strand the profile in processing.
        if (string.Equals(profileStatusBeforeRun, "complete", StringComparison.OrdinalIgnoreCase)
            && !definition.IsTerminal)
            return "complete";

        return "processing";
    }

    private async Task<(bool Success, string? Error)> FailRerunAsync(
        Guid profileId,
        Guid userId,
        NicheStepDefinition definition,
        string error,
        CancellationToken ct)
    {
        await PushStepEvent(profileId, userId, definition.Slug, definition, "error", error, ct);
        return (false, error);
    }

    private Task PushStepEvent(
        Guid profileId,
        Guid userId,
        string slug,
        NicheStepDefinition? definition,
        string status,
        string message,
        CancellationToken ct) =>
        progressNotifier.PushAsync(
            profileId,
            userId,
            slug,
            status,
            message,
            definition?.StepNumber,
            TotalSteps,
            ct);

    private static bool IsDependencyComplete(IReadOnlyDictionary<string, string> statuses, string dep)
    {
        if (statuses.TryGetValue(dep, out var depStatus)
            && string.Equals(depStatus, "complete", StringComparison.OrdinalIgnoreCase))
            return true;

        // Legacy 14-step runs stored combined crawl/structure as site_structure.
        if (string.Equals(dep, "site_crawl", StringComparison.OrdinalIgnoreCase)
            && statuses.TryGetValue("site_structure", out var legacy)
            && string.Equals(legacy, "complete", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
