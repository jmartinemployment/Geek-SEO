using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Hubs;
using GeekSeoBackend.Services.NicheExtraction;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services;

public sealed class NicheAnalyzerService(
    INicheProfileRepository profileRepo,
    IProjectRepository projectRepo,
    SchemaOrgExtractor schemaExtractor,
    SitemapExtractor sitemapExtractor,
    NavMenuExtractor navMenuExtractor,
    HomepageHeadingsExtractor headingsExtractor,
    PageContentExtractor pageContentExtractor,
    SitePageCrawler sitePageCrawler,
    InternalLinkExtractor internalLinkExtractor,
    UrlPatternExtractor urlPatternExtractor,
    TopicFusionEngine topicFusionEngine,
    PillarDemandEnricher pillarDemandEnricher,
    GscQueryExtractor gscQueryExtractor,
    NicheAuthorityScorer scorer,
    NicheRootEntityBuilder rootBuilder,
    IHubContext<SeoContentScoringHub> hub,
    ICurrentUserContext userContext,
    ILogger<NicheAnalyzerService> logger)
{
    private const int TotalSteps = 14;

    public async Task<Guid> EnqueueAsync(
        Guid userId, Guid projectId, string domain,
        string? seedTopic = null, CancellationToken ct = default)
    {
        var siteUrl = await ResolveSiteUrlAsync(projectId, domain, ct);
        var profile = new NicheProfile
        {
            ProjectId = projectId,
            Domain = siteUrl,
            Status = "queued",
            AnalysisVersion = "1.0",
            AnalysisStepLog = "[]",
            AnalysisStepLogVersion = 1,
        };

        var result = await profileRepo.CreateAsync(profile, ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to create niche profile: {result.Error}");

        return result.Value!.Id;
    }

    public async Task RunAnalysisAsync(Guid profileId, Guid userId, IBrowser? browser, CancellationToken ct)
    {
        try
        {
            var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profileResult.IsSuccess || profileResult.Value is null)
            {
                await FailAsync(userId, profileId, "Profile not found");
                return;
            }

            var profile = profileResult.Value;
            var domain = NicheSiteUrlNormalizer.Normalize(profile.Domain);

            await profileRepo.UpdateStatusAsync(profileId, "processing",
                step: "schema", stepNumber: 1, totalSteps: TotalSteps, ct: ct);

            // Step 1 — Schema.org
            var schemaData = await schemaExtractor.ExtractAsync(domain, browser, ct);
            var schemaMessage = schemaData.ServiceNames.Count > 0
                ? $"Found {schemaData.ServiceNames.Count} schema topic(s) in homepage JSON-LD ({schemaData.KnowsAboutTopics.Count} knowsAbout, {schemaData.OfferCatalogTopics.Count} offer catalog / serviceType)."
                : "Schema.org step complete — no service topics on homepage.";
            await PushProgress(
                userId, profileId, 1,
                NicheAnalysisStepLogBuilder.Schema(1, schemaData, schemaMessage),
                ct);

            // Step 2 — Site URLs (sitemap until crawl service exists)
            var sitemapData = await sitemapExtractor.ExtractAsync(domain, ct);
            var siteUrlsMessage = sitemapData.TotalUrlsScanned > 0
                ? $"Site URLs: {sitemapData.TotalUrlsScanned} from sitemap."
                : "Site URLs: none found in sitemap.";
            await PushProgress(
                userId, profileId, 2,
                NicheAnalysisStepLogBuilder.SiteUrls(2, sitemapData, siteUrlsMessage),
                ct);

            // Step 3 — Navigation
            NavMenuData navData = new([], "skipped");
            if (browser is not null)
                navData = await navMenuExtractor.ExtractAsync(domain, browser, ct);

            var navMessage = navData.ExtractMethod switch
            {
                "skipped" => "Navigation step skipped — browser unavailable.",
                _ => $"Navigation: {navData.Pillars.Count} link groups ({navData.ExtractMethod}).",
            };
            await PushProgress(
                userId, profileId, 3,
                NicheAnalysisStepLogBuilder.Nav(3, navData, navMessage),
                ct);

            // Step 4 — Homepage headings (extractor output only — no schema substitution)
            var headings = await headingsExtractor.ExtractAsync(domain, browser, ct);
            var headingsMessage =
                headings.Headings.Count > 0 || !string.IsNullOrWhiteSpace(headings.Title)
                    ? $"Headings: {headings.Headings.Count} elements from homepage."
                    : "Headings: none found on homepage.";
            await PushProgress(
                userId, profileId, 4,
                NicheAnalysisStepLogBuilder.Headings(4, headings, headingsMessage),
                ct);

            // Page content (peer signal — lists + section headings on homepage)
            var pageContent = await pageContentExtractor.ExtractAsync(domain, browser, ct);
            var pageParts = new List<string>();
            if (pageContent.VerticalTopics.Count > 0)
                pageParts.Add($"{pageContent.VerticalTopics.Count} H2/H3 vertical section(s)");
            if (pageContent.ServicePhrases.Count > 0)
                pageParts.Add($"{pageContent.ServicePhrases.Count} body phrase(s)");
            var pageMessage = pageParts.Count > 0
                ? $"Page content: {string.Join(", ", pageParts)} from homepage."
                : "Page content: no additional service phrases on homepage.";
            await PushProgress(
                userId, profileId, 5,
                NicheAnalysisStepLogBuilder.PageContent(5, pageContent, pageMessage),
                ct);

            // Step 6 — Site structure (multi-page crawl, internal links, URL patterns)
            var crawlData = await sitePageCrawler.CrawlAsync(domain, sitemapData.SampleUrls, browser, ct);
            var internalLinkData = internalLinkExtractor.Extract(crawlData, domain);
            var crawlUrls = crawlData.Pages.Select(p => p.Url).ToList();
            var patternUrls = sitemapData.SampleUrls
                .Concat(crawlUrls)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var urlPatternData = urlPatternExtractor.Extract(patternUrls, domain);
            var structureParts = new List<string>
            {
                $"{crawlData.PagesFetched} page(s) crawled",
                $"{internalLinkData.Links.Count} internal link(s) ({internalLinkData.Links.Count(l => !l.InferredFromUrlSlug)} anchor, {internalLinkData.Links.Count(l => l.InferredFromUrlSlug)} from URL slug)",
                $"{urlPatternData.Topics.Count} URL pattern topic(s)",
            };
            var structureMessage =
                $"Site structure: {string.Join(", ", structureParts)}.";
            await PushProgress(
                userId, profileId, 6,
                NicheAnalysisStepLogBuilder.SiteStructure(
                    6, crawlData, internalLinkData, urlPatternData, structureMessage),
                ct);

            // Step 7 — GSC owner overlay (Tier 3, optional) + fuse all signals
            var gscOverlay = await gscQueryExtractor.ExtractAsync(userId, profile.ProjectId, ct);
            var candidatePool = TopicCandidatePoolBuilder.Build(
                schemaData, sitemapData, navData, headings, pageContent,
                internalLinkData, urlPatternData);
            candidatePool = GscQueryExtractor.ApplyToPool(candidatePool, gscOverlay);
            var fused = topicFusionEngine.Fuse(
                candidatePool,
                schemaData.AreaServed.ToList());
            fused = NormalizedTopicalityCalculator.Apply(fused, crawlData, urlPatternData);
            var mergeResult = topicFusionEngine.ToPillarMergeResult(fused);
            var merged = mergeResult.Selected;
            var silentGscSlugs = GscQueryExtractor.FindSilentPillarSlugs(merged, gscOverlay);
            var gscMatchedCount = CountBySource(fused.AllCandidates, "gsc");
            var mergeMessage = BuildMergeMessage(mergeResult, fused, gscOverlay, gscMatchedCount, silentGscSlugs);
            await PushProgress(
                userId, profileId, 7,
                NicheAnalysisStepLogBuilder.Merging(
                    7,
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
                    fused.FusionVersion,
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
                ct);

            var projectResult = await projectRepo.GetByIdAsync(profile.ProjectId, ct);
            var keywordLocation = projectResult.IsSuccess
                && !string.IsNullOrWhiteSpace(projectResult.Value?.DefaultLocation)
                ? projectResult.Value.DefaultLocation
                : "United States";

            // Step 8 — Keyword demand (Tier 2, optional)
            var demand = await pillarDemandEnricher.EnrichAsync(
                merged, profileId, domain, keywordLocation, ct);
            merged = demand.PillarsAfterDemotion.ToList();
            var keywordsMessage = demand.KeywordsSkipped
                ? $"Keyword demand skipped — {demand.KeywordSkipReason ?? "provider unavailable"}."
                : $"Keyword demand: enriched {demand.Keywords.Count(k => k.Enriched)} of {demand.Keywords.Count} pillar(s) via {demand.KeywordProvider}.";
            await PushProgress(
                userId, profileId, 8,
                NicheAnalysisStepLogBuilder.Keywords(8, demand, keywordsMessage),
                ct);

            // Step 9 — SERP validation + competitors (Tier 2, optional)
            var serpMessage = demand.SerpSkipped
                ? $"SERP validation skipped — {demand.SerpSkipReason ?? "provider unavailable"}."
                : demand.DemotedSlugs.Count > 0
                    ? $"SERP validation ({demand.SerpProvider}): {demand.SerpValidations.Count(v => v.HasSerpFootprint)} pillar(s) with footprint, {demand.DemotedSlugs.Count} demoted, {demand.Competitors.Count} competitor(s) found."
                    : $"SERP validation ({demand.SerpProvider}): {demand.SerpValidations.Count(v => v.HasSerpFootprint)} pillar(s) with organic footprint, {demand.Competitors.Count} competitor(s) found.";
            await PushProgress(
                userId, profileId, 9,
                NicheAnalysisStepLogBuilder.SerpValidation(9, demand, serpMessage),
                ct);

            // Step 10 — Niche identity
            var nicheEntities = BuildNichePillars(merged, profileId, demand.Keywords);
            var rootEntity = rootBuilder.Build(schemaData, headings, nicheEntities);
            var audienceType = DetermineAudienceType(nicheEntities, schemaData);
            var nicheTags = BuildNicheTags(schemaData, nicheEntities).ToArray();
            var profileMessage = $"Niche profile: {rootEntity}.";
            await PushProgress(
                userId, profileId, 10,
                NicheAnalysisStepLogBuilder.Profile(10, rootEntity, audienceType, nicheTags, profileMessage),
                ct);

            // Step 11 — Local geography (areaServed vs location pages)
            var localGeo = LocalGapGenerator.Analyze(
                schemaData,
                sitemapData,
                crawlUrls,
                urlPatternData,
                merged);
            fused = fused with { LocalGeography = localGeo };

            var localMessage = !localGeo.IsLocalBusiness
                ? "Local geography: no areaServed or location URLs detected."
                : localGeo.Gaps.Count > 0
                    ? $"Local geography: {localGeo.AreasServed.Count} area(s) declared, {localGeo.LocationPagesFound.Count} location page(s), {localGeo.Gaps.Count} gap(s)."
                    : localGeo.AreasServed.Count > 0
                        ? $"Local geography: {localGeo.AreasServed.Count} area(s) declared — all have matching location pages."
                        : $"Local geography: {localGeo.LocationPagesFound.Count} location page(s) found.";
            await PushProgress(
                userId, profileId, 11,
                localGeo.IsLocalBusiness
                    ? NicheAnalysisStepLogBuilder.Local(11, localGeo, localMessage)
                    : NicheAnalysisStepLogBuilder.LocalDisabled(11, localMessage),
                ct);

            // Step 12 — Content coverage (fusion + crawl → pillar/subtopic status)
            foreach (var pillar in nicheEntities)
            {
                if (pillar.Id == Guid.Empty)
                    pillar.Id = Guid.NewGuid();
            }

            var subtopics = BuildSubtopics(nicheEntities, merged);
            AttachSubtopics(nicheEntities, subtopics);

            var coverageResult = NicheContentCoverageMatcher.Apply(
                nicheEntities,
                subtopics,
                fused,
                merged,
                crawlData,
                sitemapData,
                demand.SerpValidations);

            scorer.ScorePillars(nicheEntities);

            var coverageMessage =
                $"Content coverage: {coverageResult.PillarsCovered} covered, {coverageResult.PillarsPartial} partial, {coverageResult.PillarsGap} gap — {coverageResult.SubtopicsCovered}/{coverageResult.SubtopicsTotal} subtopics matched to URLs.";
            await PushProgress(
                userId, profileId, 12,
                NicheAnalysisStepLogBuilder.Coverage(
                    12,
                    coverageResult.PillarsCovered,
                    coverageResult.PillarsPartial,
                    coverageResult.PillarsGap,
                    coverageResult.SubtopicsCovered,
                    coverageResult.SubtopicsTotal,
                    coverageResult.SamplePartialPillars,
                    coverageMessage),
                ct);

            // Step 13 — Authority score + persist
            var authorityScore = scorer.ComputeTopicalAuthorityScore(nicheEntities);
            var covered = nicheEntities.Count(p => p.CoverageStatus == "covered");
            var partial = nicheEntities.Count(p => p.CoverageStatus == "partial");
            var gap = nicheEntities.Count(p => p.CoverageStatus == "gap");

            foreach (var pillar in nicheEntities)
            {
                if (pillar.Id == Guid.Empty)
                    pillar.Id = Guid.NewGuid();
            }

            var pillarsResult = await profileRepo.BulkInsertPillarsAsync(nicheEntities, ct);
            if (!pillarsResult.IsSuccess)
                throw new InvalidOperationException($"Failed to save pillars: {pillarsResult.Error}");

            var subtopicsResult = await profileRepo.BulkInsertSubtopicsAsync(subtopics, ct);
            if (!subtopicsResult.IsSuccess)
            {
                logger.LogWarning(
                    "Subtopics not saved for {ProfileId}: {Error}",
                    profileId, subtopicsResult.Error);
            }

            if (demand.Competitors.Count > 0)
            {
                var competitorsResult = await profileRepo.BulkInsertCompetitorsAsync(demand.Competitors, ct);
                if (!competitorsResult.IsSuccess)
                {
                    logger.LogWarning(
                        "Competitors not saved for {ProfileId}: {Error}",
                        profileId, competitorsResult.Error);
                }
            }

            fused = FusionSnapshotEnricher.Apply(
                fused, internalLinkData, urlPatternData, demand.SerpValidations);

            var analyzedAt = DateTimeOffset.UtcNow;
            var nextDue = analyzedAt.AddDays(30);
            var saveResult = await profileRepo.SaveAnalysisResultsAsync(profileId, new NicheAnalysisSaveRequest(
                rootEntity,
                schemaData.Description ?? string.Empty,
                nicheTags,
                audienceType,
                string.Empty,
                authorityScore,
                nicheEntities.Count,
                covered,
                partial,
                gap,
                analyzedAt,
                nextDue,
                FusedSiteUnderstandingJson.Serialize(fused)), ct);
            if (!saveResult.IsSuccess)
                throw new InvalidOperationException($"Failed to save analysis results: {saveResult.Error}");

            var scoringMessage = $"Authority score: {authorityScore:F0}/100 — results saved.";
            var entityThinCount = fused.EntityCoverageBySlug.Values.Count(c => c.IsEntityThin);
            var linkGraphEdgeCount = fused.InternalLinkGraph?.Edges.Count ?? 0;
            var orphanPillarCount = fused.InternalLinkGraph?.OrphanSlugs.Count ?? 0;
            await PushProgress(
                userId, profileId, 13,
                NicheAnalysisStepLogBuilder.Scoring(
                    13,
                    authorityScore,
                    covered,
                    partial,
                    gap,
                    nicheEntities.Count,
                    scoringMessage,
                    entityThinCount,
                    linkGraphEdgeCount,
                    orphanPillarCount,
                    fused.RecommendedActions.Count),
                ct);

            const string completeMessage = "Analysis complete!";
            await PushProgress(
                userId, profileId, TotalSteps,
                NicheAnalysisStepLogBuilder.Complete(TotalSteps, analyzedAt, nextDue, completeMessage),
                ct);
            await profileRepo.UpdateStatusAsync(profileId, "complete", "complete", TotalSteps, TotalSteps, ct: ct);

            logger.LogInformation(
                "Niche analysis complete for {ProfileId}: {Niche}, {Pillars} pillars, score {Score}",
                profileId, rootEntity, nicheEntities.Count, authorityScore);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Niche analysis failed for {ProfileId}", profileId);
            var message = ex is OperationCanceledException
                ? "Analysis timed out. Click Re-analyze to run again."
                : ex.Message;
            await FailAsync(userId, profileId, message, ct);
        }
    }

    private async Task FailAsync(Guid userId, Guid profileId, string error, CancellationToken ct = default)
    {
        await profileRepo.UpdateStatusAsync(profileId, "failed", errorMessage: error, ct: ct);
        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync("AnalysisProgress", new
            {
                ProfileId = profileId,
                Step = "failed",
                StepNumber = 0,
                TotalSteps,
                Message = error,
                Status = "failed",
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push failure notification for {ProfileId}", profileId);
        }
    }

    private async Task PushProgress(
        Guid userId,
        Guid profileId,
        int stepNumber,
        NicheAnalysisStepLogEntry stepEntry,
        CancellationToken ct = default)
    {
        var status = stepNumber >= TotalSteps ? "complete" : "processing";
        try
        {
            await profileRepo.UpdateStatusAsync(
                profileId, status, stepEntry.Slug, stepNumber, TotalSteps, stepLogEntry: stepEntry, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to persist niche step {Step} for {ProfileId}", stepEntry.Slug, profileId);
        }

        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync("AnalysisProgress", new
            {
                ProfileId = profileId,
                Step = stepEntry.Slug,
                StepNumber = stepNumber,
                TotalSteps,
                Message = stepEntry.Summary,
                Status = stepNumber >= TotalSteps ? "complete" : "processing",
            });
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SignalR push failed for {ProfileId} step {Step}", profileId, stepEntry.Slug);
        }
    }

    private static IReadOnlyList<DiscoveredPillar> BuildSchemaDiscoveredPillars(SchemaOrgData schema) =>
        schema.ServiceNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(name => new DiscoveredPillar
            {
                Name = name,
                Slug = NicheAnalyzerService.NameToSlug(name),
                Intent = "commercial",
                Source = "schema",
                ChildPageCount = 3,
            })
            .ToList();

    private static List<NichePillar> BuildNichePillars(
        IReadOnlyList<DiscoveredPillar> merged,
        Guid profileId,
        IReadOnlyList<PillarKeywordEnrichment> keywordMetrics)
    {
        var metricsBySlug = keywordMetrics
            .Where(k => k.Enriched)
            .ToDictionary(k => k.Slug, StringComparer.OrdinalIgnoreCase);

        return merged.Select((p, idx) =>
        {
            metricsBySlug.TryGetValue(p.Slug, out var metrics);
            return new NichePillar
            {
                NicheProfileId = profileId,
                PillarTopic = p.Name,
                PillarSlug = p.Slug,
                PrimaryKeyword = metrics?.Keyword ?? p.Name.ToLowerInvariant(),
                PageUrl = p.PageUrl,
                SearchIntent = p.Intent,
                Source = p.Source,
                DisplayOrder = idx,
                CoverageStatus = "gap",
                RequiredSubtopicCount = Math.Max(p.ChildPageCount, 5),
                SearchVolume = metrics?.SearchVolume ?? 0,
                KeywordDifficulty = metrics?.KeywordDifficulty ?? 0m,
            };
        }).ToList();
    }

    private static List<NicheSubtopic> BuildSubtopics(
        List<NichePillar> pillars,
        IReadOnlyList<DiscoveredPillar> discovered)
    {
        var subtopics = new List<NicheSubtopic>();
        var discMap = discovered.ToDictionary(d => d.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var pillar in pillars)
        {
            if (!discMap.TryGetValue(pillar.PillarSlug, out var disc)) continue;

            var childSlugs = disc.ChildSlugs.Take(10).ToList();
            foreach (var childSlug in childSlugs)
            {
                subtopics.Add(new NicheSubtopic
                {
                    PillarId = pillar.Id,
                    SubtopicTitle = SitemapExtractor.SlugToTitle(childSlug),
                    TargetKeyword = $"{pillar.PrimaryKeyword} {childSlug.Replace('-', ' ')}".Trim(),
                    SearchIntent = pillar.SearchIntent == "local" ? "local" : "informational",
                    CoverageStatus = "gap",
                    RecommendedFormat = InferFormat(childSlug),
                    FixEffort = "create",
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
                    subtopics.Add(new NicheSubtopic
                    {
                        PillarId = pillar.Id,
                        SubtopicTitle = $"{pillar.PillarTopic} – {SitemapExtractor.SlugToTitle(suffix)}",
                        TargetKeyword = $"{pillar.PrimaryKeyword} {suffix.Replace('-', ' ')}".Trim(),
                        SearchIntent = intent,
                        CoverageStatus = "gap",
                        RecommendedFormat = format,
                        FixEffort = "create",
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

    private static string DetermineAudienceType(
        List<NichePillar> pillars,
        SchemaOrgData schema)
    {
        var hasLocalPillars = pillars.Any(p => p.SearchIntent == "local");
        var hasLocationArea = schema.AreaServed.Count > 0;

        if (hasLocalPillars || hasLocationArea) return "local_service";

        var hasInfoPillars = pillars.Count(p => p.SearchIntent == "informational");
        if (hasInfoPillars > pillars.Count / 2) return "blog";

        return "local_service";
    }

    private static IEnumerable<string> BuildNicheTags(
        SchemaOrgData schema,
        List<NichePillar> pillars)
    {
        var tags = new List<string>();
        tags.AddRange(schema.AreaServed.Take(3));
        tags.AddRange(pillars.Take(3).Select(p => p.PillarTopic));
        return tags.Distinct(StringComparer.OrdinalIgnoreCase).Take(8);
    }

    internal static string NameToSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(
            name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

    private static string BuildMergeMessage(
        PillarMergeResult mergeResult,
        FusedSiteUnderstanding fused,
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

    private static string[] SampleExclusionReasons(FusedSiteUnderstanding fused) =>
        fused.ExclusionReasons
            .Take(20)
            .Select(kvp => $"{kvp.Key}: {kvp.Value}")
            .ToArray();

    private async Task<string> ResolveSiteUrlAsync(
        Guid projectId, string domainFromRequest, CancellationToken ct)
    {
        var projectResult = await projectRepo.GetByIdAsync(projectId, ct);
        if (projectResult.IsSuccess && !string.IsNullOrWhiteSpace(projectResult.Value?.Url))
            return NicheSiteUrlNormalizer.Normalize(projectResult.Value.Url);

        return NicheSiteUrlNormalizer.Normalize(domainFromRequest);
    }
}
