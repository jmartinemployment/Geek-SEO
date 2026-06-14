using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;
using GeekSeoBackend.Auth;
using GeekSeoBackend.Hubs;
using GeekSeoBackend.Services.NicheExtraction;
using GeekSeoBackend.Services.NicheStepRunners;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Playwright;

namespace GeekSeoBackend.Services;

public sealed class NicheAnalyzerService(
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
    NicheStepExecutionService stepExecution,
    IHubContext<SeoContentScoringHub> hub,
    ICurrentUserContext userContext,
    ILogger<NicheAnalyzerService> logger)
{
    private const int TotalSteps = 14;
    private static bool FusionArchiveEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("NICHE_FUSION_ARCHIVE_ENABLED"),
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
            if (s is "queued" or "processing")
                return latest.Value.Id;

            var allPending = NicheStepCatalog.Ordered
                .Select(step => step.Slug)
                .ToList();

            try
            {
                await profileRepo.UpdateStatusAsync(
                    latest.Value.Id,
                    "queued",
                    step: null,
                    stepNumber: 0,
                    totalSteps: TotalSteps,
                    errorMessage: null,
                    ct: ct);
                await profileRepo.UpdatePhaseStatusAsync(
                    latest.Value.Id,
                    new NichePhaseStatusPatch(
                        StructureStatus: "pending",
                        EnrichmentStatus: "pending",
                        PersistStage: null,
                        Status: "queued"),
                    ct);
                await ClearRelationalCrawlUrlsAsync(latest.Value.Id, ct);
                await profileRepo.InvalidateDownstreamStepsAsync(latest.Value.Id, allPending, ct);
            }
            catch
            {
                // Best-effort reset; worker-run initialization remains authoritative.
            }

            return latest.Value.Id;
        }

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
            var domain = NicheSiteUrlNormalizer.Normalize(profile.Domain);
            await InitializeStepStatusesAsync(profileId, ct);
            await profileRepo.UpdateStatusAsync(
                profileId,
                "processing",
                step: NicheStepCatalog.Ordered[0].Slug,
                stepNumber: 1,
                totalSteps: TotalSteps,
                ct: ct);

            foreach (var step in NicheStepCatalog.Ordered)
            {
                var entry = await stepExecution.RunAsync(step.Slug, profileId, userId, domain, browser, ct);
                await PushProgress(userId, profileId, step.StepNumber, entry, ct);
            }

            await profileRepo.UpdatePhaseStatusAsync(
                profileId,
                new NichePhaseStatusPatch(
                    StructureStatus: "complete",
                    EnrichmentStatus: "complete",
                    PersistStage: "done",
                    Status: "complete"),
                ct);
            await profileRepo.UpdateStatusAsync(profileId, "complete", "complete", TotalSteps, TotalSteps, ct: ct);
            logger.LogInformation("Niche analysis complete for {ProfileId}", profileId);
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
        var failedStep = _lastProgressStepNumber > 0 ? _lastProgressStepSlug : "failed";
        var failedStepNumber = _lastProgressStepNumber > 0 ? _lastProgressStepNumber : 0;

        await profileRepo.UpdateStatusAsync(
            profileId,
            "failed",
            step: failedStep,
            stepNumber: failedStepNumber,
            totalSteps: TotalSteps,
            errorMessage: error,
            ct: ct);
        if (!string.IsNullOrWhiteSpace(_lastProgressStepSlug) && _lastProgressStepSlug != "failed")
        {
            try
            {
                var errorEntry = new NicheAnalysisStepLogEntry(
                    _lastProgressStepNumber,
                    _lastProgressStepSlug,
                    NicheStepCatalog.BySlug.TryGetValue(_lastProgressStepSlug, out var failedStepDef)
                        ? failedStepDef.Title
                        : _lastProgressStepSlug,
                    "error",
                    error,
                    new Dictionary<string, object?>());
                await profileRepo.UpdateStepStatusAsync(profileId, _lastProgressStepSlug, "error", errorEntry, ct: ct);
            }
            catch { /* non-fatal */ }
        }
        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync("AnalysisProgress", new
            {
                ProfileId = profileId,
                Step = "failed",
                StepNumber = failedStepNumber,
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

    private async Task InitializeStepStatusesAsync(Guid profileId, CancellationToken ct)
    {
        var allPending = NicheStepCatalog.Ordered
            .Select(step => step.Slug)
            .ToList();
        try { await ClearRelationalCrawlUrlsAsync(profileId, ct); } catch { /* non-fatal */ }
        try { await profileRepo.InvalidateDownstreamStepsAsync(profileId, allPending, ct); } catch { /* non-fatal */ }
    }

    private async Task PushProgress(
        Guid userId,
        Guid profileId,
        int stepNumber,
        NicheAnalysisStepLogEntry stepEntry,
        CancellationToken ct = default)
    {
        _lastProgressStepSlug = stepEntry.Slug;
        _lastProgressStepNumber = stepNumber;

        var stepStatus = stepNumber >= TotalSteps ? "complete" : "complete";
        var overallStatus = stepNumber >= TotalSteps ? "complete" : "processing";
        try
        {
            await profileRepo.UpdateStatusAsync(
                profileId, overallStatus, stepEntry.Slug, stepNumber, TotalSteps, stepLogEntry: stepEntry, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist niche step {Step} (step {StepNumber}) for profile {ProfileId}",
                stepEntry.Slug, stepNumber, profileId);
        }

        // Write per-step status for isolation
        try
        {
            await profileRepo.UpdateStepStatusAsync(profileId, stepEntry.Slug, stepStatus, stepEntry, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Step status update failed for {Slug}", stepEntry.Slug);
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
                PaaQuestionsJson = ToJson(serp?.PaaQuestions),
                RelatedSearchesJson = ToJson(serp?.RelatedSearches),
                LocalPaaQuestionsJson = ToJson(serp?.LocalPaaQuestions),
                LocalRelatedSearchesJson = ToJson(serp?.LocalRelatedSearches),
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
            .Take(20)
            .Select(kvp => $"{kvp.Key}: {kvp.Value}")
            .ToArray();

    private async Task<IReadOnlyList<string>?> LoadPriorSitemapUrlsWithFallbackAsync(
        NicheProfile profile,
        CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            return await LoadPriorSitemapUrlsAsync(profile, cts.Token);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<string>?> LoadPriorSitemapUrlsAsync(
        NicheProfile profile,
        CancellationToken ct)
    {
        var discoveredUrls = await profileRepo.GetDiscoveredUrlsAsync(profile.Id, ct);
        if (!discoveredUrls.IsSuccess)
            return null;

        return (discoveredUrls.Value ?? [])
            .Where(x => string.Equals(x.SourceType, "sitemap", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task ClearRelationalCrawlUrlsAsync(Guid profileId, CancellationToken ct)
    {
        var discoveredUrls = await profileRepo.GetDiscoveredUrlsAsync(profileId, ct);
        if (!discoveredUrls.IsSuccess)
            return;

        var preservedUrls = (discoveredUrls.Value ?? [])
            .Where(x => !string.Equals(x.SourceType, "crawl", StringComparison.OrdinalIgnoreCase))
            .Select(x => new NicheProfileDiscoveredUrlWrite(x.Url, x.SourceType, x.LastSeenAt))
            .ToList();

        await profileRepo.ReplaceDiscoveredUrlsAsync(profileId, preservedUrls, ct);
    }

    private async Task<string> ResolveSiteUrlAsync(
        Guid projectId, string domainFromRequest, CancellationToken ct)
    {
        var projectResult = await projectRepo.GetByIdAsync(projectId, ct);
        if (projectResult.IsSuccess && !string.IsNullOrWhiteSpace(projectResult.Value?.Url))
            return NicheSiteUrlNormalizer.Normalize(projectResult.Value.Url);

        return NicheSiteUrlNormalizer.Normalize(domainFromRequest);
    }
}
