using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Infrastructure;
using GeekSeoBackend.Services.SiteExtraction;
using GeekSeoBackend.Services.SiteAnalyzerStepRunners;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services;

public sealed class SiteAnalyzerService(
    ISiteAnalysisProfileRepository profileRepo,
    IProjectRepository projectRepo,
    SiteAnalysisPersistenceService persistence,
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
    SiteAuthorityScorer scorer,
    SiteRootEntityBuilder rootBuilder,
    SiteAnalyzerStepExecutionService stepExecution,
    SiteAnalysisProgressNotifier progressNotifier,
    SiteAnalysisJobChannel jobChannel,
    ICurrentUserContext userContext,
    ILogger<SiteAnalyzerService> logger)
{
    private static int TotalSteps => SiteAnalyzerStepCatalog.ThroughCoverage.Count;
    private static bool FusionArchiveEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("SITE_ANALYSIS_FUSION_ARCHIVE_ENABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    private string _lastProgressStepSlug = "schema";
    private int _lastProgressStepNumber;

    public async Task<Guid> EnqueueAsync(
        Guid userId, Guid projectId, string domain,
        string? seedTopic = null, CancellationToken ct = default)
    {
        var latest = await profileRepo.GetLatestByProjectAsync(projectId, ct);
        if (latest.IsSuccess && latest.Value is not null)
        {
            var s = latest.Value.Status;
            if (s is "processing")
                return latest.Value.Id;

            try
            {
                await ResetForManualRunAsync(latest.Value.Id, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Best-effort manual-run reset failed for profile {ProfileId}", latest.Value.Id);
            }

            return latest.Value.Id;
        }

        var siteUrl = await ResolveSiteUrlAsync(projectId, domain, ct);
        var profile = new SiteAnalysisProfile
        {
            ProjectId = projectId,
            Domain = siteUrl,
            Status = "pending",
            AnalysisVersion = "2.0",
            AnalysisStepLog = "[]",
            AnalysisStepLogVersion = 1,
            AnalysisTotalSteps = SiteAnalyzerStepCatalog.ThroughCoverage.Count,
        };

        var result = await profileRepo.CreateAsync(profile, ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to create site analysis profile: {result.Error}");

        var profileId = result.Value!.Id;
        try
        {
            await ResetForManualRunAsync(profileId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Manual-run initialization failed for new profile {ProfileId}", profileId);
        }

        return profileId;
    }

    /// <summary>
    /// Queues a Through Coverage crawl for Content Creator. No profile row until persist.
    /// </summary>
    public async Task QueueSiteAnalysisAsync(
        Guid userId,
        Guid projectId,
        string domain,
        string? seedTopic = null,
        CancellationToken ct = default)
    {
        if (!IsWorkerConfigured())
            throw new InvalidOperationException("Site analysis worker is not running");

        var siteUrl = await ResolveSiteUrlAsync(projectId, domain, ct);
        jobChannel.Enqueue(new ThroughCoverageJob(userId, projectId, siteUrl, seedTopic));
        logger.LogInformation(
            "Queued through-coverage crawl for project {ProjectId} domain {Domain}",
            projectId,
            siteUrl);
    }

    public static bool IsWorkerConfigured()
    {
        var raw = Environment.GetEnvironmentVariable("WORKER_SERVICE_USER_ID");
        return Guid.TryParse(raw, out var id) && id != Guid.Empty;
    }

    /// <summary>
    /// Runs site-model spine through coverage only (no market/SERP/scoring finalize).
    /// </summary>
    public async Task RunThroughCoverageAsync(Guid profileId, Guid userId, IBrowser? browser, CancellationToken ct)
    {
        _lastProgressStepSlug = "schema";
        _lastProgressStepNumber = 0;
        var total = SiteAnalyzerStepCatalog.ThroughCoverage.Count;

        try
        {
            var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profileResult.IsSuccess || profileResult.Value is null)
            {
                await FailAsync(userId, profileId, "Profile not found", totalStepsOverride: total);
                return;
            }

            var profile = profileResult.Value;
            var domain = SiteUrlNormalizer.Normalize(profile.Domain);
            await InitializeStepStatusesAsync(profileId, ct);
            await profileRepo.UpdatePhaseStatusAsync(
                profileId,
                new SiteAnalysisPhaseStatusPatch(
                    PersistStage: SiteAnalyzerStepCatalog.SiteCoveragePersistStage,
                    Status: "processing"),
                ct);
            await profileRepo.UpdateStatusAsync(
                profileId,
                "processing",
                step: SiteAnalyzerStepCatalog.ThroughCoverage[0],
                stepNumber: 1,
                totalSteps: total,
                ct: ct);

            var stepNumber = 0;
            foreach (var slug in SiteAnalyzerStepCatalog.ThroughCoverage)
            {
                stepNumber++;
                _lastProgressStepSlug = slug;
                _lastProgressStepNumber = stepNumber;
                var entry = await stepExecution.RunAsync(slug, profileId, userId, domain, browser, ct);
                if (string.Equals(entry.Status, "error", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(entry.Summary ?? $"Site analysis stage '{slug}' failed");

                await PushProgress(userId, profileId, stepNumber, entry, ct, totalStepsOverride: total);
            }

            await profileRepo.UpdatePhaseStatusAsync(
                profileId,
                new SiteAnalysisPhaseStatusPatch(
                    StructureStatus: "complete",
                    EnrichmentStatus: "complete",
                    PersistStage: SiteAnalyzerStepCatalog.SiteCoveragePersistStage,
                    Status: "complete"),
                ct);
            await profileRepo.UpdateStatusAsync(
                profileId,
                "complete",
                SiteAnalyzerStepCatalog.ThroughCoverage[^1],
                total,
                total,
                ct: ct);
            logger.LogInformation("Site analysis (through coverage) complete for {ProfileId}", profileId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Site analysis failed for {ProfileId}", profileId);
            var message = ex is OperationCanceledException
                ? "Analysis timed out."
                : ex.Message;
            await FailAsync(userId, profileId, message, ct, totalStepsOverride: total);
        }
    }

    public async Task RunAnalysisAsync(Guid profileId, Guid userId, IBrowser? browser, CancellationToken ct)
    {
        _lastProgressStepSlug = "schema";
        _lastProgressStepNumber = 0;

        try
        {
            var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profileResult.IsSuccess || profileResult.Value is null)
            {
                await FailAsync(userId, profileId, "Profile not found");
                return;
            }

            var profile = profileResult.Value;
            var domain = SiteUrlNormalizer.Normalize(profile.Domain);
            await InitializeStepStatusesAsync(profileId, ct);
            await profileRepo.UpdateStatusAsync(
                profileId,
                "processing",
                step: SiteAnalysisStepCatalog.Ordered[0].Slug,
                stepNumber: 1,
                totalSteps: TotalSteps,
                ct: ct);

            foreach (var step in SiteAnalysisStepCatalog.Ordered)
            {
                var entry = await stepExecution.RunAsync(step.Slug, profileId, userId, domain, browser, ct);
                await PushProgress(userId, profileId, step.StepNumber, entry, ct);
            }

            await profileRepo.UpdatePhaseStatusAsync(
                profileId,
                new SiteAnalysisPhaseStatusPatch(
                    StructureStatus: "complete",
                    EnrichmentStatus: "complete",
                    PersistStage: "done",
                    Status: "complete"),
                ct);
            await profileRepo.UpdateStatusAsync(profileId, "complete", "complete", TotalSteps, TotalSteps, ct: ct);
            logger.LogInformation("site analysis complete for {ProfileId}", profileId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "site analysis failed for {ProfileId}", profileId);
            var message = ex is OperationCanceledException
                ? "Analysis timed out. Click Re-analyze to run again."
                : ex.Message;
            await FailAsync(userId, profileId, message, ct);
        }
    }

    private async Task FailAsync(
        Guid userId,
        Guid profileId,
        string error,
        CancellationToken ct = default,
        int? totalStepsOverride = null)
    {
        var failedStep = _lastProgressStepNumber > 0 ? _lastProgressStepSlug : "failed";
        var failedStepNumber = _lastProgressStepNumber > 0 ? _lastProgressStepNumber : 0;
        var totalSteps = totalStepsOverride ?? TotalSteps;

        await profileRepo.UpdateStatusAsync(
            profileId,
            "failed",
            step: failedStep,
            stepNumber: failedStepNumber,
            totalSteps: totalSteps,
            errorMessage: error,
            ct: ct);
        if (!string.IsNullOrWhiteSpace(_lastProgressStepSlug) && _lastProgressStepSlug != "failed")
        {
            try
            {
                var errorEntry = new SiteAnalysisStepLogEntry(
                    _lastProgressStepNumber,
                    _lastProgressStepSlug,
                    SiteAnalysisStepCatalog.BySlug.TryGetValue(_lastProgressStepSlug, out var failedStepDef)
                        ? failedStepDef.Title
                        : _lastProgressStepSlug,
                    "error",
                    error,
                    new Dictionary<string, object?>());
                await profileRepo.UpdateStepStatusAsync(profileId, _lastProgressStepSlug, "error", errorEntry, ct: ct);
                await SiteAnalyzerStepRunStatusWriter.SyncAsync(
                    profileRepo, logger, profileId, _lastProgressStepSlug, "error", failedStepDef, errorEntry, error, ct);
            }
            catch { /* non-fatal */ }
        }
        await progressNotifier.PushAsync(
            profileId,
            userId,
            "failed",
            "failed",
            error,
            failedStepNumber,
            totalSteps,
            ct);
    }

    private async Task InitializeStepStatusesAsync(Guid profileId, CancellationToken ct) =>
        await ResetForManualRunAsync(profileId, ct);

    /// <summary>
    /// Prepares a profile for manual step-by-step execution — all steps pending, no worker queue.
    /// </summary>
    private async Task ResetForManualRunAsync(Guid profileId, CancellationToken ct)
    {
        var allSlugs = SiteAnalysisStepCatalog.Ordered.Select(step => step.Slug).ToList();

        await profileRepo.UpdateStatusAsync(
            profileId,
            "pending",
            step: null,
            stepNumber: 0,
            totalSteps: TotalSteps,
            errorMessage: null,
            ct: ct);
        await profileRepo.UpdatePhaseStatusAsync(
            profileId,
            new SiteAnalysisPhaseStatusPatch(
                StructureStatus: "pending",
                EnrichmentStatus: "pending",
                PersistStage: null,
                Status: "pending"),
            ct);
        await ClearRelationalCrawlUrlsAsync(profileId, ct);
        await profileRepo.InvalidateDownstreamStepsAsync(profileId, allSlugs, ct);
        await EnsurePendingStepRunsAsync(profileId, ct);
    }

    private async Task EnsurePendingStepRunsAsync(Guid profileId, CancellationToken ct)
    {
        foreach (var step in SiteAnalysisStepCatalog.Ordered)
        {
            var upsert = await profileRepo.UpsertStepRunAsync(
                profileId,
                new SiteAnalysisProfileStepRunUpsert(step.StepNumber, step.Slug, "pending"),
                ct);
            if (!upsert.IsSuccess)
            {
                logger.LogWarning(
                    "Could not upsert pending step run {Slug} for {ProfileId}: {Error}",
                    step.Slug,
                    profileId,
                    upsert.Error);
            }
        }
    }

    private async Task PushProgress(
        Guid userId,
        Guid profileId,
        int stepNumber,
        SiteAnalysisStepLogEntry stepEntry,
        CancellationToken ct = default,
        int? totalStepsOverride = null)
    {
        _lastProgressStepSlug = stepEntry.Slug;
        _lastProgressStepNumber = stepNumber;
        var totalSteps = totalStepsOverride ?? TotalSteps;

        var stepStatus = "complete";
        var overallStatus = stepNumber >= totalSteps ? "complete" : "processing";
        try
        {
            await profileRepo.UpdateStatusAsync(
                profileId, overallStatus, stepEntry.Slug, stepNumber, totalSteps, stepLogEntry: stepEntry, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist site analysis step {Step} (step {StepNumber}) for profile {ProfileId}",
                stepEntry.Slug, stepNumber, profileId);
        }

        // Write per-step status for isolation
        try
        {
            await profileRepo.UpdateStepStatusAsync(profileId, stepEntry.Slug, stepStatus, stepEntry, ct: ct);
            SiteAnalysisStepCatalog.BySlug.TryGetValue(stepEntry.Slug, out var stepDef);
            await SiteAnalyzerStepRunStatusWriter.SyncAsync(
                profileRepo, logger, profileId, stepEntry.Slug, stepStatus, stepDef, stepEntry, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Step status update failed for {Slug}", stepEntry.Slug);
        }

        await progressNotifier.PushAsync(
            profileId,
            userId,
            stepEntry.Slug,
            stepNumber >= totalSteps ? "complete" : "processing",
            stepEntry.Summary,
            stepNumber,
            totalSteps,
            ct);
    }

    private static IReadOnlyList<DiscoveredPillar> BuildSchemaDiscoveredPillars(SchemaOrgData schema) =>
        schema.ServiceNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(name => new DiscoveredPillar
            {
                Name = name,
                Slug = SiteAnalyzerService.NameToSlug(name),
                Intent = "commercial",
                Source = "schema",
                ChildPageCount = 3,
            })
            .ToList();


    private static List<SiteAnalysisPillar> BuildSiteAnalysisPillars(
        IReadOnlyList<DiscoveredPillar> merged,
        Guid profileId,
        IReadOnlyList<PillarKeywordEnrichment> keywordMetrics,
        IReadOnlyList<PillarSerpEnrichment> serpValidations)
    {
        var metricsBySlug = keywordMetrics
            .Where(k => k.Enriched)
            .ToDictionary(k => k.Slug, StringComparer.OrdinalIgnoreCase);

        var serpBySlug = serpValidations
            .ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);

        return merged.Select((p, idx) =>
        {
            metricsBySlug.TryGetValue(p.Slug, out var metrics);
            serpBySlug.TryGetValue(p.Slug, out var serp);

            static string ToJson<T>(IReadOnlyList<T>? list) =>
                list is { Count: > 0 } ? System.Text.Json.JsonSerializer.Serialize(list) : "[]";

            return new SiteAnalysisPillar
            {
                SiteAnalysisProfileId = profileId,
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
                PaaQuestionsJson = ToJson(serp?.PaaQuestions),
                RelatedSearchesJson = ToJson(serp?.RelatedSearches),
                LocalPaaQuestionsJson = ToJson(serp?.LocalPaaQuestions),
                LocalRelatedSearchesJson = ToJson(serp?.LocalRelatedSearches),
            };
        }).ToList();
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

    private static string DetermineAudienceType(
        List<SiteAnalysisPillar> pillars,
        SchemaOrgData schema)
    {
        var hasLocalPillars = pillars.Any(p => p.SearchIntent == "local");
        var hasLocationArea = schema.AreaServed.Count > 0;

        if (hasLocalPillars || hasLocationArea) return "local_service";

        var hasInfoPillars = pillars.Count(p => p.SearchIntent == "informational");
        if (hasInfoPillars > pillars.Count / 2) return "blog";

        return "local_service";
    }

    private static IEnumerable<string> BuildFocusTags(
        SchemaOrgData schema,
        List<SiteAnalysisPillar> pillars)
    {
        var tags = new List<string>();
        tags.AddRange(schema.AreaServed);
        tags.AddRange(pillars.Select(p => p.PillarTopic));
        return tags.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    internal static string NameToSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(
            name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

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

    private static string[] SampleExclusionReasons(SiteTopicProfile fused) =>
        fused.ExclusionReasons
            .Select(kvp => $"{kvp.Key}: {kvp.Value}")
            .ToArray();


    private async Task ClearRelationalCrawlUrlsAsync(Guid profileId, CancellationToken ct)
    {
        var discoveredUrls = await profileRepo.GetDiscoveredUrlsAsync(profileId, ct);
        if (!discoveredUrls.IsSuccess)
            return;

        var preservedUrls = (discoveredUrls.Value ?? [])
            .Where(x => !string.Equals(x.SourceType, "crawl", StringComparison.OrdinalIgnoreCase))
            .Select(x => new SiteAnalysisProfileDiscoveredUrlWrite(x.Url, x.SourceType, x.LastSeenAt))
            .ToList();

        await profileRepo.ReplaceDiscoveredUrlsAsync(profileId, preservedUrls, ct);
    }

    private async Task<string> ResolveSiteUrlAsync(
        Guid projectId, string domainFromRequest, CancellationToken ct)
    {
        var projectResult = await projectRepo.GetByIdAsync(projectId, ct);
        if (projectResult.IsSuccess && !string.IsNullOrWhiteSpace(projectResult.Value?.Url))
            return SiteUrlNormalizer.Normalize(projectResult.Value.Url);

        return SiteUrlNormalizer.Normalize(domainFromRequest);
    }
}
