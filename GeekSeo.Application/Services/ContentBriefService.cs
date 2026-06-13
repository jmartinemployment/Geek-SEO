using System.Text.Json;
using GeekSeo.Application.Interfaces;
using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class ContentBriefService(
    IProjectRepository projects,
    ISerpCacheRepository serpCache,
    ISerpProvider serpProvider,
    IAIProvider ai,
    INicheProfileRepository nicheProfiles,
    INicheAnalyticsDapperRepository nicheAnalytics,
    CompetitorCrawlService competitorCrawl) : IContentBriefService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Dictionary<string, string> SoftwareEntityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zapier"] = "Zapier",
        ["quickbooks"] = "QuickBooks",
        ["hubspot"] = "HubSpot",
        ["salesforce"] = "Salesforce",
        ["shopify"] = "Shopify",
        ["mailchimp"] = "Mailchimp",
        ["stripe"] = "Stripe",
        ["xero"] = "Xero",
    };

    public async Task<Result<ContentBrief>> GenerateBriefAsync(
        Guid userId, GenerateBriefRequest request, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(request.ProjectId, ct);
        if (!project.IsSuccess || project.Value is null || project.Value.UserId != userId)
            return Result<ContentBrief>.Failure("Access denied");

        var keyword = request.Keyword.Trim();
        var location = string.IsNullOrWhiteSpace(request.Location)
            ? project.Value.DefaultLocation
            : request.Location;
        const string languageCode = "en";

        var serpRow = await EnsureSerpAsync(keyword, location, languageCode, ct);
        if (!serpRow.IsSuccess)
            return Result<ContentBrief>.Failure(serpRow.Error ?? "SERP error");
        if (serpRow.Value is null)
            return Result<ContentBrief>.Failure("Could not load SERP data for this keyword");

        var benchmarks = JsonSerializer.Deserialize<SerpBenchmarksPayload>(serpRow.Value.ResultsJson, JsonOptions)
            ?? new SerpBenchmarksPayload();
        var paa = JsonSerializer.Deserialize<List<PeopleAlsoAskResult>>(serpRow.Value.PeopleAlsoAskJson, JsonOptions) ?? [];
        var related = JsonSerializer.Deserialize<List<string>>(serpRow.Value.RelatedSearchesJson, JsonOptions) ?? [];

        var competitors = benchmarks.OrganicResults.Take(request.CompetitorCount).Select(o => new BriefCompetitorSummary
        {
            Position = o.Position,
            Url = o.Url,
            Title = o.Title,
            WordCount = CountWords(o.Snippet) * 12,
        }).ToList();

        var terms = await BuildRecommendedTermsAsync(keyword, benchmarks, related, ct);
        var headings = BuildSuggestedHeadings(keyword, paa);
        var latestProfile = await TryGetLatestProfileAsync(request.ProjectId, ct);
        var gapTopics = await TryGetGapTopicsAsync(latestProfile?.Id, ct);
        var matchedPillar = FindMatchedPillar(keyword, latestProfile);
        var competitorDomains = BuildCompetitorDomains(benchmarks, latestProfile);
        var geoAnchorNodes = BuildGeoAnchorNodes(location, project.Value.BusinessAddress, project.Value.DefaultLocation);
        var softwareEntities = ExtractSoftwareEntities(keyword, latestProfile, terms);
        var competitorPages = await TryGetCompetitorPagesAsync(serpRow.Value, benchmarks.OrganicResults, ct);
        var competitorHeadingHighlights = ExtractCompetitorHeadingHighlights(competitorPages);
        var competitorSchemaTypes = ExtractCompetitorSchemaTypes(competitorPages);

        return Result<ContentBrief>.Success(new ContentBrief
        {
            Keyword = keyword,
            Location = location,
            TargetWordCount = benchmarks.AvgWordCount,
            AvgTitleLength = benchmarks.AvgTitleLength,
            RecommendedTerms = terms,
            SuggestedHeadings = headings,
            TopCompetitors = competitors,
            CompetitorDomains = competitorDomains,
            CompetitorHeadingHighlights = competitorHeadingHighlights,
            CompetitorSchemaTypes = competitorSchemaTypes,
            PeopleAlsoAsk = paa.Select(p => p.Question).Where(q => q.Length > 0).ToList(),
            Methodology = WritingMethodologySpec.FourPhase,
            DirectAnswerBlocks =
            [
                new DirectAnswerBlockSpec(
                    "Direct answer",
                    "Open with a concise definition and business outcome before expanding into methodology and implementation detail."),
            ],
            TechnicalEvidenceRequirements =
            [
                "Include sanitized code or webhook examples when the topic is technical.",
                "Reference software versions, workflow constraints, or implementation assumptions when they materially affect execution.",
            ],
            GeoAnchorNodes = geoAnchorNodes,
            SchemaBlueprint = new SchemaBlueprint
            {
                PrimaryType = "TechArticle",
                AdditionalTypes = ["FAQPage"],
                SoftwareEntities = softwareEntities,
                AboutEntities = BuildAboutEntities(latestProfile, matchedPillar, geoAnchorNodes),
            },
            ReviewChecklist =
            [
                "Verify software versions and code logic before publication.",
                "Confirm direct-answer blocks are factual and concise.",
                "Check local references against the target service area.",
            ],
            NicheContext = new NicheContextSpec
            {
                PrimaryNiche = latestProfile?.PrimaryNiche,
                MatchedPillar = matchedPillar,
                GapTopics = gapTopics,
            },
            SerpIntelligence = new SerpIntelligenceSnapshot
            {
                PeopleAlsoAsk = paa.Select(p => p.Question).Where(q => q.Length > 0).ToList(),
                RelatedSearches = related,
                FeatureFlags = ExtractSerpFeatureFlags(serpRow.Value.SerpFeaturesJson),
                FeaturedSnippet = string.IsNullOrWhiteSpace(serpRow.Value.FeaturedSnippet)
                    ? null
                    : serpRow.Value.FeaturedSnippet,
            },
            AuthorOrganizationName = project.Value.Name,
            AuthorOrganizationUrl = project.Value.Url,
            BenchmarkQuality = benchmarks.BenchmarkQuality,
        });
    }

    private async Task<Result<SeoSerpResult?>> EnsureSerpAsync(
        string keyword, string location, string languageCode, CancellationToken ct)
    {
        var cache = await serpCache.GetAsync(keyword, location, languageCode, ct);
        if (!cache.IsSuccess)
            return Result<SeoSerpResult?>.Failure(cache.Error ?? "SERP cache error");

        if (cache.Value is not null && cache.Value.ExpiresAt > DateTimeOffset.UtcNow)
            return Result<SeoSerpResult?>.Success(cache.Value);

        var fetch = await serpProvider.GetSerpResultsAsync(new SerpRequest
        {
            Keyword = keyword,
            Location = location,
            LanguageCode = languageCode,
            ResultCount = 10,
        }, ct);

        if (!fetch.IsSuccess || fetch.Value is null)
            return Result<SeoSerpResult?>.Failure(fetch.Error ?? "SERP fetch failed");

        var benchmarks = SerpBenchmarkCalculator.FromSerp(fetch.Value);
        var upserted = await serpCache.UpsertAsync(keyword, location, languageCode, fetch.Value, benchmarks, ct);
        return upserted.IsSuccess
            ? Result<SeoSerpResult?>.Success(upserted.Value)
            : Result<SeoSerpResult?>.Failure(upserted.Error ?? "SERP upsert failed");
    }

    private async Task<IReadOnlyList<string>> BuildRecommendedTermsAsync(
        string keyword,
        SerpBenchmarksPayload benchmarks,
        List<string> related,
        CancellationToken ct)
    {
        var snippets = benchmarks.OrganicResults
            .Take(5)
            .Select(o => $"{o.Title}: {o.Snippet}")
            .ToList();

        var aiResult = await ai.CompleteAsync(new AIRequest
        {
            SystemPrompt =
                "You extract SEO semantic terms. Respond with a JSON array of 8-12 short phrases only, no markdown.",
            UserPrompt =
                $"Keyword: {keyword}\n\nCompetitor snippets:\n{string.Join("\n", snippets)}\n\nRelated: {string.Join(", ", related.Take(8))}",
            MaxTokens = 512,
            Temperature = 0.3,
        }, ct);

        if (aiResult.IsSuccess && aiResult.Value is not null)
        {
            try
            {
                var terms = JsonSerializer.Deserialize<List<string>>(aiResult.Value.Content.Trim());
                if (terms is { Count: > 0 })
                    return terms;
            }
            catch
            {
                // fall through
            }
        }

        var fallback = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        fallback.AddRange(related.Take(5));
        return fallback.Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
    }

    private static List<string> BuildSuggestedHeadings(string keyword, List<PeopleAlsoAskResult> paa)
    {
        var list = new List<string>
        {
            $"What is {keyword}?",
            $"Benefits of {keyword}",
            $"How to choose {keyword}",
            "Frequently asked questions",
        };
        list.AddRange(paa.Take(3).Select(p => p.Question));
        return list.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private async Task<NicheProfile?> TryGetLatestProfileAsync(Guid projectId, CancellationToken ct)
    {
        var profileResult = await nicheProfiles.GetLatestByProjectAsync(projectId, ct);
        return profileResult.IsSuccess ? profileResult.Value : null;
    }

    private async Task<IReadOnlyList<string>> TryGetGapTopicsAsync(Guid? profileId, CancellationToken ct)
    {
        if (profileId is null)
            return [];

        var gaps = await nicheAnalytics.GetTopicalGapsAsync(profileId.Value, quickWinsOnly: false, ct);
        return gaps.IsSuccess && gaps.Value is not null
            ? gaps.Value.Select(g => g.SubtopicTitle).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList()
            : [];
    }

    private async Task<IReadOnlyList<SeoCompetitorPage>> TryGetCompetitorPagesAsync(
        SeoSerpResult serpRow,
        IReadOnlyList<SerpOrganicResult> organicResults,
        CancellationToken ct)
    {
        var pages = await competitorCrawl.EnsureCompetitorPagesAsync(serpRow.Id, organicResults, ct);
        return pages.IsSuccess && pages.Value is not null ? pages.Value : [];
    }

    private static string? FindMatchedPillar(string keyword, NicheProfile? profile)
    {
        if (profile?.Pillars is null || profile.Pillars.Count == 0)
            return null;

        var normalizedKeyword = keyword.ToLowerInvariant();
        var direct = profile.Pillars.FirstOrDefault(p =>
            normalizedKeyword.Contains(p.PillarTopic, StringComparison.OrdinalIgnoreCase)
            || normalizedKeyword.Contains(p.PrimaryKeyword, StringComparison.OrdinalIgnoreCase)
            || p.PrimaryKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(token => normalizedKeyword.Contains(token, StringComparison.OrdinalIgnoreCase)));

        return direct?.PillarTopic ?? profile.Pillars.First().PillarTopic;
    }

    private static IReadOnlyList<string> BuildCompetitorDomains(SerpBenchmarksPayload benchmarks, NicheProfile? profile)
    {
        var domains = benchmarks.OrganicResults
            .Select(o => o.Domain)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d!)
            .Concat(profile?.Competitors.Select(c => c.Domain) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        return domains;
    }

    private static IReadOnlyList<string> BuildGeoAnchorNodes(string location, string? businessAddress, string? defaultLocation) =>
        new[] { location, businessAddress, defaultLocation }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IReadOnlyList<string> ExtractSoftwareEntities(
        string keyword,
        NicheProfile? profile,
        IReadOnlyList<string> recommendedTerms)
    {
        var candidates = new List<string>();
        AddMatchingSoftwareEntities(candidates, keyword);
        foreach (var tag in profile?.NicheTags ?? [])
            AddMatchingSoftwareEntities(candidates, tag);
        foreach (var term in recommendedTerms)
            AddMatchingSoftwareEntities(candidates, term);

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
    }

    private static void AddMatchingSoftwareEntities(List<string> destination, string text)
    {
        foreach (var pair in SoftwareEntityMap)
        {
            if (text.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                destination.Add(pair.Value);
        }
    }

    private static IReadOnlyList<string> BuildAboutEntities(
        NicheProfile? profile,
        string? matchedPillar,
        IReadOnlyList<string> geoAnchorNodes)
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(profile?.PrimaryNiche))
            values.Add(profile.PrimaryNiche);
        if (!string.IsNullOrWhiteSpace(matchedPillar))
            values.Add(matchedPillar);
        values.AddRange(geoAnchorNodes);

        return values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static IReadOnlyList<string> ExtractCompetitorHeadingHighlights(IReadOnlyList<SeoCompetitorPage> pages)
    {
        var headings = new List<string>();
        foreach (var page in pages)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(page.HeadingsJson, JsonOptions) ?? [];
                headings.AddRange(parsed);
            }
            catch
            {
                // ignore malformed cache entries
            }
        }

        return headings
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static IReadOnlyList<string> ExtractCompetitorSchemaTypes(IReadOnlyList<SeoCompetitorPage> pages)
    {
        var schemaTypes = new List<string>();
        foreach (var page in pages)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(page.StructuredDataTypesJson, JsonOptions) ?? [];
                schemaTypes.AddRange(parsed);
            }
            catch
            {
                // ignore malformed cache entries
            }
        }

        return schemaTypes
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static IReadOnlyList<string> ExtractSerpFeatureFlags(string serpFeaturesJson)
    {
        if (string.IsNullOrWhiteSpace(serpFeaturesJson))
            return [];

        try
        {
            var features = JsonSerializer.Deserialize<Dictionary<string, object?>>(serpFeaturesJson, JsonOptions) ?? [];
            return features.Keys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
