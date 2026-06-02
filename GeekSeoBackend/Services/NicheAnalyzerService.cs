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
    PillarMerger pillarMerger,
    PillarValidator pillarValidator,
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
        var profile = new NicheProfile
        {
            ProjectId = projectId,
            Domain = NormalizeDomain(domain),
            Status = "queued",
            AnalysisVersion = "1.0",
        };

        var result = await profileRepo.CreateAsync(profile, ct);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to create niche profile: {result.Error}");

        return result.Value!.Id;
    }

    public async Task RunAnalysisAsync(Guid profileId, Guid userId, IBrowser? browser, CancellationToken ct)
    {
        await PushProgress(userId, profileId, "schema", 1, "Extracting schema.org data…", ct);

        try
        {
            // Load project to get domain + location
            var profileResult = await profileRepo.GetByIdAsync(profileId, ct);
            if (!profileResult.IsSuccess || profileResult.Value is null)
            {
                await FailAsync(userId, profileId, "Profile not found");
                return;
            }

            var profile = profileResult.Value;
            var domain = EnsureHttps(profile.Domain);

            await profileRepo.UpdateStatusAsync(profileId, "processing",
                step: "schema", stepNumber: 1, totalSteps: TotalSteps, ct: ct);

            // Step 1 — Schema.org
            var schemaData = await schemaExtractor.ExtractAsync(domain, ct);
            await PushProgress(userId, profileId, "sitemap", 2, "Parsing sitemap…", ct);

            // Step 2 — Sitemap
            var sitemapData = await sitemapExtractor.ExtractAsync(domain, ct);
            await PushProgress(userId, profileId, "nav", 3, "Crawling navigation menu…", ct);

            // Step 3 — Nav menu (Playwright)
            NavMenuData navData = new([], "skipped");
            if (browser is not null)
            {
                navData = await navMenuExtractor.ExtractAsync(domain, browser, ct);
            }
            await PushProgress(userId, profileId, "headings", 4, "Extracting homepage headings…", ct);

            // Step 4 — Title + meta from sitemap/schema (no Playwright needed)
            var headings = BuildHeadingsFromSchema(schemaData);
            await PushProgress(userId, profileId, "merging", 5, "Merging pillar signals…", ct);

            // Step 5 — Merge all sources
            var schemaPillars = BuildSchemaDiscoveredPillars(schemaData);
            var merged = pillarMerger.Merge(
                schemaPillars,
                sitemapData.Pillars,
                navData.Pillars,
                [],
                schemaData.AreaServed.ToList());

            await PushProgress(userId, profileId, "validating", 6, "Validating pillars…", ct);

            // Step 6 — Root entity + niche string
            var nicheEntities = BuildNichePillars(merged, profileId);
            scorer.ScorePillars(nicheEntities);

            var rootEntity = rootBuilder.Build(schemaData, headings, nicheEntities);
            var discoveryMethod = DetermineDiscovery(schemaPillars, sitemapData, navData);

            // Step 7 — Determine audience type
            var audienceType = DetermineAudienceType(nicheEntities, schemaData);
            await PushProgress(userId, profileId, "scoring", 7, "Computing topical authority score…", ct);

            // Step 8 — Score
            var authorityScore = scorer.ComputeTopicalAuthorityScore(nicheEntities);
            var covered = nicheEntities.Count(p => p.CoverageStatus == "covered");
            var partial = nicheEntities.Count(p => p.CoverageStatus == "partial");
            var gap = nicheEntities.Count(p => p.CoverageStatus == "gap");

            await PushProgress(userId, profileId, "saving", 8, "Saving analysis results…", ct);

            // Step 9 — Persist
            profile.PrimaryNiche = rootEntity;
            profile.NicheDescription = schemaData.Description ?? string.Empty;
            profile.NicheTags = BuildNicheTags(schemaData, nicheEntities).ToArray();
            profile.AudienceType = audienceType;
            profile.DiscoveryMethod = discoveryMethod;
            profile.TotalPillarsIdentified = nicheEntities.Count;
            profile.AnalyzedAt = DateTimeOffset.UtcNow;
            profile.NextAnalysisDue = DateTimeOffset.UtcNow.AddDays(30);

            await profileRepo.BulkInsertPillarsAsync(nicheEntities, ct);

            var subtopics = BuildSubtopics(nicheEntities, merged);
            await profileRepo.BulkInsertSubtopicsAsync(subtopics, ct);

            await profileRepo.UpdateScoresAsync(profileId, authorityScore, covered, partial, gap, ct);

            await PushProgress(userId, profileId, "complete", TotalSteps, "Analysis complete!", ct);
            await profileRepo.UpdateStatusAsync(profileId, "complete", "complete", TotalSteps, TotalSteps, ct: ct);

            logger.LogInformation(
                "Niche analysis complete for {ProfileId}: {Niche}, {Pillars} pillars, score {Score}",
                profileId, rootEntity, nicheEntities.Count, authorityScore);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Niche analysis failed for {ProfileId}", profileId);
            await FailAsync(userId, profileId, ex.Message, ct);
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
        string step,
        int stepNumber,
        string message,
        CancellationToken ct = default)
    {
        var status = stepNumber >= TotalSteps ? "complete" : "processing";
        try
        {
            await profileRepo.UpdateStatusAsync(
                profileId, status, step, stepNumber, TotalSteps, ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to persist niche step {Step} for {ProfileId}", step, profileId);
        }

        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync("AnalysisProgress", new
            {
                ProfileId = profileId,
                Step = step,
                StepNumber = stepNumber,
                TotalSteps,
                Message = message,
                Status = stepNumber >= TotalSteps ? "complete" : "processing",
            });
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SignalR push failed for {ProfileId} step {Step}", profileId, step);
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

            // Always add at least 5 generic subtopics for pillars with no children
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

    private static HomepageHeadings BuildHeadingsFromSchema(SchemaOrgData schema) =>
        new(schema.BrandName, schema.Description, []);

    private static string DetermineDiscovery(
        IReadOnlyList<DiscoveredPillar> schemaPillars,
        SitemapData sitemap,
        NavMenuData nav)
    {
        if (schemaPillars.Count > 0) return "schema";
        if (sitemap.Pillars.Count > 0) return "sitemap";
        if (nav.Pillars.Count > 0) return "nav";
        return "fallback";
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

    private static string NormalizeDomain(string domain)
    {
        if (!domain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            domain = "https://" + domain;
        try { return new Uri(domain).GetLeftPart(UriPartial.Authority); }
        catch { return domain.TrimEnd('/'); }
    }

    private static string EnsureHttps(string domain)
    {
        if (domain.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return domain;
        return "https://" + domain;
    }
}
