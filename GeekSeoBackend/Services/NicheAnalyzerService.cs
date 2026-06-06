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
    TopicFusionEngine topicFusionEngine,
    NicheAuthorityScorer scorer,
    NicheRootEntityBuilder rootBuilder,
    IHubContext<SeoContentScoringHub> hub,
    ICurrentUserContext userContext,
    ILogger<NicheAnalyzerService> logger)
{
    private const int TotalSteps = 10;

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
            var pageMessage = pageContent.ServicePhrases.Count > 0
                ? $"Page content: {pageContent.ServicePhrases.Count} service-like phrase(s) from homepage."
                : "Page content: no additional service phrases on homepage.";

            // Step 5 — Fuse all Tier-1 signals
            var candidatePool = TopicCandidatePoolBuilder.Build(
                schemaData, sitemapData, navData, headings, pageContent);
            var fused = topicFusionEngine.Fuse(
                candidatePool,
                schemaData.AreaServed.ToList());
            var mergeResult = topicFusionEngine.ToPillarMergeResult(fused);
            var merged = mergeResult.Selected;
            var mergeMessage =
                mergeResult.ExcludedByCap.Count > 0
                    ? $"Topic pillars: {merged.Count} selected, {mergeResult.ExcludedByCap.Count} held back (cap {mergeResult.PillarCap}). Fused {fused.AllCandidates.Count} peer candidate(s) ({string.Join(", ", fused.SignalSourcesPresent)})."
                    : $"Topic pillars: {merged.Count} after fusion of {fused.AllCandidates.Count} peer candidate(s) ({string.Join(", ", fused.SignalSourcesPresent)}).";
            await PushProgress(
                userId, profileId, 5,
                NicheAnalysisStepLogBuilder.Merging(
                    5,
                    fused.AllCandidates.Count,
                    merged.Count,
                    merged,
                    CountBySource(fused.AllCandidates, "schema"),
                    CountBySource(fused.AllCandidates, "sitemap"),
                    CountBySource(fused.AllCandidates, "nav"),
                    CountBySource(fused.AllCandidates, "heading"),
                    mergeResult.ExcludedByCap,
                    mergeResult.PillarCap,
                    pageContent.ServicePhrases.Count,
                    fused.FusionVersion,
                    SampleExclusionReasons(fused),
                    mergeMessage),
                ct);

            // Step 6 — Niche identity
            var nicheEntities = BuildNichePillars(merged, profileId);
            scorer.ScorePillars(nicheEntities);
            var rootEntity = rootBuilder.Build(schemaData, headings, nicheEntities);
            var audienceType = DetermineAudienceType(nicheEntities, schemaData);
            var nicheTags = BuildNicheTags(schemaData, nicheEntities).ToArray();
            var profileMessage = $"Niche profile: {rootEntity}.";
            await PushProgress(
                userId, profileId, 6,
                NicheAnalysisStepLogBuilder.Profile(6, rootEntity, audienceType, nicheTags, profileMessage),
                ct);

            // Step 7 — Local geography (progress only until LocalGapGenerator ships)
            const string localMessage = "Local geography: not enabled in this release.";
            await PushProgress(
                userId, profileId, 7,
                NicheAnalysisStepLogBuilder.LocalDisabled(7, localMessage),
                ct);

            // Step 8 — Content coverage (progress only until coverage matcher ships)
            const string coverageMessage = "Content coverage: not enabled in this release.";
            await PushProgress(
                userId, profileId, 8,
                NicheAnalysisStepLogBuilder.CoverageDisabled(8, coverageMessage),
                ct);

            // Step 9 — Authority score + persist
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

            var subtopics = BuildSubtopics(nicheEntities, merged);
            var subtopicsResult = await profileRepo.BulkInsertSubtopicsAsync(subtopics, ct);
            if (!subtopicsResult.IsSuccess)
            {
                logger.LogWarning(
                    "Subtopics not saved for {ProfileId}: {Error}",
                    profileId, subtopicsResult.Error);
            }

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
                nextDue), ct);
            if (!saveResult.IsSuccess)
                throw new InvalidOperationException($"Failed to save analysis results: {saveResult.Error}");

            var scoringMessage = $"Authority score: {authorityScore:F0}/100 — results saved.";
            await PushProgress(
                userId, profileId, 9,
                NicheAnalysisStepLogBuilder.Scoring(
                    9, authorityScore, covered, partial, gap, nicheEntities.Count, scoringMessage),
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
        IReadOnlyList<DiscoveredPillar> merged, Guid profileId)
    {
        return merged.Select((p, idx) => new NichePillar
        {
            NicheProfileId = profileId,
            PillarTopic = p.Name,
            PillarSlug = p.Slug,
            PrimaryKeyword = p.Name.ToLowerInvariant(),
            PageUrl = p.PageUrl,
            SearchIntent = p.Intent,
            Source = p.Source,
            DisplayOrder = idx,
            CoverageStatus = "gap",
            RequiredSubtopicCount = Math.Max(p.ChildPageCount, 5),
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
