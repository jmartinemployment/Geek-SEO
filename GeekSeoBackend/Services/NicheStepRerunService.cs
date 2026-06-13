using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Hubs;
using GeekSeoBackend.Services.NicheExtraction;
using GeekSeoBackend.Services.NicheStepRunners;
using Microsoft.AspNetCore.SignalR;
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
    IHubContext<SeoContentScoringHub> hub,
    ILogger<NicheStepRerunService> logger)
{
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

        // Load current step statuses
        var statusResult = await profileRepo.GetStepStatusesAsync(profileId, ct);
        if (!statusResult.IsSuccess)
            return (false, $"Could not load step statuses: {statusResult.Error}");
        var statuses = statusResult.Value;

        foreach (var dep in definition.Dependencies)
        {
            if (!statuses.TryGetValue(dep, out var depStatus) || depStatus != "complete")
                return (false, $"Dependency '{dep}' must be complete before running '{slug}'.");
        }

        // Acquire per-profile lock
        var sem = stepLock.Get(profileId);
        if (!await sem.WaitAsync(0, ct))
            return (false, "Another step is already running for this profile.");

        try
        {
            var downstream = NicheStepCatalog.GetDownstream(slug);
            if (downstream.Count > 0)
                await profileRepo.InvalidateDownstreamStepsAsync(profileId, downstream, ct);

            // Mark this step running
            await profileRepo.UpdateStepStatusAsync(profileId, slug, "running", ct: ct);
            await PushStepEvent(profileId, userId, slug, "running", $"Re-running step: {slug}…", ct);

            var entry = await ExecuteStepAsync(profileId, userId, slug, browser, ct);

            // Mark complete
            await profileRepo.UpdateStepStatusAsync(profileId, slug, "complete", entry, ct);
            await PushStepEvent(profileId, userId, slug, "complete", entry.Summary, ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Step re-run failed for profile {ProfileId} slug {Slug}", profileId, slug);
            await profileRepo.UpdateStepStatusAsync(profileId, slug, "error", ct: ct);
            await PushStepEvent(profileId, userId, slug, "error", $"Step '{slug}' failed: {ex.Message}", ct);
            return (false, ex.Message);
        }
        finally
        {
            sem.Release();
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

        // Persist updated URL list
        await profileRepo.UpdateCrawledUrlsAsync(profileId,
            JsonSerializer.Serialize(crawlUrls), ct);

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

        await profileRepo.UpdateCrawledUrlsAsync(profileId, JsonSerializer.Serialize(crawlUrls), ct);

        var gsc = await gscQueryExtractor.ExtractAsync(userId, profile.ProjectId, ct);
        var pool = TopicCandidatePoolBuilder.Build(schema, sitemap, nav, headings, content, links, patterns);
        pool = GscQueryExtractor.ApplyToPool(pool, gsc);
        var fused = pillarSelector.Select(pool, schema.AreaServed.ToList());
        fused = NormalizedTopicalityCalculator.Apply(fused, crawlData, patterns);
        var mergeResult = pillarSelector.ToPillarMergeResult(fused);
        var merged = mergeResult.Selected;
        var silent = GscQueryExtractor.FindSilentPillarSlugs(merged, gsc);
        var gscCount = pool.Count(c => c.Evidence.Any(e => e.Source == "gsc"));

        await persistence.PersistCandidatesAsync(profileId, fused, includeEvidence: false, ct);
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
        if (!details.IsSuccess || details.Value?.FusionSnapshot is null)
            throw new InvalidOperationException("Fusion snapshot not found — run merging step first.");
        return SiteTopicProfileJson.Parse(details.Value.FusionSnapshot)
            ?? throw new InvalidOperationException("Could not parse fusion snapshot.");
    }

    private async Task<List<string>> LoadCrawledUrlsAsync(Guid profileId, CancellationToken ct)
    {
        var profile = await profileRepo.GetByIdAsync(profileId, ct);
        var json = profile.Value?.CrawledUrlsJson;
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonSerializer.Deserialize<List<string>>(json, JsonOpts) ?? [];
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

    private async Task PushStepEvent(
        Guid profileId, Guid userId, string slug, string status, string message, CancellationToken ct)
    {
        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync("AnalysisProgress", new
            {
                ProfileId = profileId,
                Step = slug,
                Status = status,
                Message = message,
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SignalR push failed for step re-run {Slug}", slug);
        }
    }
}
