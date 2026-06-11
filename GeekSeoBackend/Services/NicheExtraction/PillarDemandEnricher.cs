using System.Collections.Concurrent;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Tier-2 demand validation: keyword metrics, SERP footprint, competitor domains (Phase C).
/// </summary>
public sealed class PillarDemandEnricher(
    IKeywordProvider keywordProvider,
    ISerpProvider serpProvider,
    ILogger<PillarDemandEnricher> logger)
{
    private const int MaxConcurrency = 8;

    public async Task<PillarDemandEnrichment> EnrichAsync(
        IReadOnlyList<DiscoveredPillar> pillars,
        Guid profileId,
        string siteDomain,
        string location,
        Func<int, int, string, Task>? onProgress = null,
        CancellationToken ct = default)
    {
        var targets = pillars.ToList();
        var siteHost = NormalizeHost(siteDomain);

        var (keyword, serp) = await RunBothPhasesAsync(targets, siteHost, location, onProgress, ct);
        var competitors = BuildCompetitors(profileId, siteHost, serp.Validations);
        var demoted = ApplySerpDemotions(pillars, serp.Validations, out var demotedSlugs);

        return new PillarDemandEnrichment(
            keyword.Enrichments,
            serp.Validations,
            competitors,
            demoted,
            demotedSlugs,
            keyword.Skipped,
            serp.Skipped,
            keyword.SkipReason,
            serp.SkipReason,
            keywordProvider.ProviderName,
            serpProvider.ProviderName);
    }

    internal static IReadOnlyList<DiscoveredPillar> ApplySerpDemotions(
        IReadOnlyList<DiscoveredPillar> pillars,
        IReadOnlyList<PillarSerpEnrichment> serpEnrichments,
        out IReadOnlyList<string> demotedSlugs)
    {
        var demoted = new List<string>();
        if (serpEnrichments.Count == 0)
        {
            demotedSlugs = demoted;
            return pillars;
        }

        var noFootprint = serpEnrichments
            .Where(s => !s.HasSerpFootprint)
            .Select(s => s.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var kept = new List<DiscoveredPillar>();
        foreach (var pillar in pillars)
        {
            if (noFootprint.Contains(pillar.Slug)
                && !string.Equals(pillar.Source, "schema", StringComparison.OrdinalIgnoreCase))
            {
                demoted.Add(pillar.Slug);
                continue;
            }

            kept.Add(pillar);
        }

        demotedSlugs = demoted;
        return kept;
    }

    internal static IReadOnlyList<NicheCompetitor> BuildCompetitors(
        Guid profileId,
        string siteHost,
        IReadOnlyList<PillarSerpEnrichment> serpEnrichments)
    {
        if (serpEnrichments.Count == 0)
            return [];

        var presence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rankingPillars = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var validation in serpEnrichments)
        {
            foreach (var domain in validation.TopCompetitorDomains)
            {
                if (string.IsNullOrWhiteSpace(domain)
                    || IsSameSite(domain, siteHost))
                    continue;

                presence[domain] = presence.GetValueOrDefault(domain) + 1;
                if (!rankingPillars.TryGetValue(domain, out var slugs))
                {
                    slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    rankingPillars[domain] = slugs;
                }

                slugs.Add(validation.Slug);
            }
        }

        return presence
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp =>
            {
                var count = kvp.Value;
                var pillarsRanking = rankingPillars.GetValueOrDefault(kvp.Key)?.Count ?? 0;
                return new NicheCompetitor
                {
                    Id = Guid.NewGuid(),
                    NicheProfileId = profileId,
                    Domain = kvp.Key,
                    SerpPresence = count,
                    PillarsRanking = pillarsRanking,
                    StrengthAssessment = AssessStrength(count),
                    EstimatedAuthorityScore = Math.Min(count * 15m, 100m),
                };
            })
            .ToList();
    }

    internal static KeywordResult? PickBestKeywordMatch(
        string pillarKeyword,
        IReadOnlyList<KeywordResult> suggestions)
    {
        if (suggestions.Count == 0)
            return null;

        var exact = suggestions.FirstOrDefault(k =>
            string.Equals(k.Keyword, pillarKeyword, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;

        return suggestions
            .OrderByDescending(k => k.SearchVolume)
            .FirstOrDefault();
    }

    private async Task<(
        (IReadOnlyList<PillarKeywordEnrichment> Enrichments, bool Skipped, string? SkipReason) Keyword,
        (IReadOnlyList<PillarSerpEnrichment> Validations, bool Skipped, string? SkipReason) Serp)>
        RunBothPhasesAsync(
            IReadOnlyList<DiscoveredPillar> targets,
            string siteHost,
            string location,
            Func<int, int, string, Task>? onProgress,
            CancellationToken ct)
    {
        var keyword = (
            Enrichments: (IReadOnlyList<PillarKeywordEnrichment>)[],
            Skipped: true,
            SkipReason: (string?)"Keyword vendor disabled — poor signal quality.");
        var serp = await ValidateSerpAsync(targets, siteHost, location, onProgress, ct);
        return (keyword, serp);
    }

    private async Task<(IReadOnlyList<PillarKeywordEnrichment> Enrichments, bool Skipped, string? SkipReason)>
        EnrichKeywordsAsync(
            IReadOnlyList<DiscoveredPillar> pillars,
            string location,
            Func<int, int, string, Task>? onProgress,
            CancellationToken ct)
    {
        var enrichments = new ConcurrentBag<PillarKeywordEnrichment>();
        var failures = 0;
        var attempted = 0;
        var completed = 0;
        using var gate = new SemaphoreSlim(MaxConcurrency);

        var tasks = pillars.Select(async pillar =>
        {
            await gate.WaitAsync(ct);
            try
            {
                Interlocked.Increment(ref attempted);
                var seed = pillar.Name.Trim();
                var result = await keywordProvider.GetKeywordSuggestionsAsync(seed, location, count: 10, ct);
                if (!result.IsSuccess || result.Value is null || result.Value.Count == 0)
                {
                    Interlocked.Increment(ref failures);
                    enrichments.Add(new PillarKeywordEnrichment(
                        pillar.Slug, seed, 0, 0m, false, result.Error));
                    return;
                }

                var match = PickBestKeywordMatch(seed.ToLowerInvariant(), result.Value);
                if (match is null)
                {
                    enrichments.Add(new PillarKeywordEnrichment(pillar.Slug, seed, 0, 0m, false, null));
                    return;
                }

                enrichments.Add(new PillarKeywordEnrichment(
                    pillar.Slug,
                    match.Keyword,
                    match.SearchVolume,
                    (decimal)match.KeywordDifficulty,
                    true,
                    null));
            }
            finally
            {
                gate.Release();
                var done = Interlocked.Increment(ref completed);
                if (onProgress is not null && (done % 5 == 0 || done == pillars.Count))
                    await onProgress(done, pillars.Count, "keywords");
            }
        });

        await Task.WhenAll(tasks);

        var pillarIndex = pillars.Select((p, i) => (p.Slug, i))
            .ToDictionary(x => x.Slug, x => x.i, StringComparer.OrdinalIgnoreCase);
        var ordered = enrichments
            .OrderBy(e => pillarIndex.GetValueOrDefault(e.Slug, int.MaxValue))
            .ToList();

        if (ordered.Count == 0)
            return ([], true, "No pillars to enrich.");

        if (attempted > 0 && failures == attempted)
        {
            var reason = ordered.FirstOrDefault(e => e.Error is not null)?.Error ?? "Keyword provider unavailable.";
            logger.LogInformation("Keyword enrichment skipped: {Reason}", reason);
            return (ordered, true, reason);
        }

        return (ordered, false, null);
    }

    private async Task<(IReadOnlyList<PillarSerpEnrichment> Validations, bool Skipped, string? SkipReason)>
        ValidateSerpAsync(
            IReadOnlyList<DiscoveredPillar> pillars,
            string siteHost,
            string location,
            Func<int, int, string, Task>? onProgress,
            CancellationToken ct)
    {
        var validations = new ConcurrentBag<PillarSerpEnrichment>();
        var failures = 0;
        var attempted = 0;
        var completed = 0;
        string? firstError = null;
        using var gate = new SemaphoreSlim(MaxConcurrency);

        var tasks = pillars.Select(async pillar =>
        {
            await gate.WaitAsync(ct);
            try
            {
                Interlocked.Increment(ref attempted);
                var keyword = pillar.Name.Trim();
                var request = new SerpRequest
                {
                    Keyword = keyword,
                    Location = location,
                    ResultCount = 10,
                };

                var result = await serpProvider.GetSerpResultsAsync(request, ct);
                if (!result.IsSuccess || result.Value is null)
                {
                    Interlocked.Increment(ref failures);
                    firstError ??= result.Error;
                    validations.Add(new PillarSerpEnrichment(
                        pillar.Slug,
                        HasSerpFootprint: true,
                        OrganicResultCount: 0,
                        SiteRanks: false,
                        SitePosition: null,
                        TopCompetitorDomains: [],
                        serpProvider.ProviderName,
                        result.Error,
                        []));
                    return;
                }

                var organic = result.Value.OrganicResults;
                var expectedTopics = SerpEntityExtractor.ExtractTopicSlugs(result.Value);
                var hasFootprint = organic.Count > 0;
                int? sitePosition = null;
                foreach (var row in organic)
                {
                    var domain = DomainFromUrl(row.Url) ?? row.Domain;
                    if (domain is null || !IsSameSite(domain, siteHost))
                        continue;

                    sitePosition = row.Position;
                    break;
                }

                var topDomains = organic
                    .Select(r => DomainFromUrl(r.Url) ?? r.Domain)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(d => d!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList();

                validations.Add(new PillarSerpEnrichment(
                    pillar.Slug,
                    hasFootprint,
                    organic.Count,
                    sitePosition.HasValue,
                    sitePosition,
                    topDomains,
                    serpProvider.ProviderName,
                    null,
                    expectedTopics,
                    result.Value.PeopleAlsoAsk.Count > 0 ? result.Value.PeopleAlsoAsk : null,
                    result.Value.RelatedSearches.Count > 0 ? result.Value.RelatedSearches : null));
            }
            finally
            {
                gate.Release();
                var done = Interlocked.Increment(ref completed);
                if (onProgress is not null && (done % 5 == 0 || done == pillars.Count))
                    await onProgress(done, pillars.Count, "serp");
            }
        });

        await Task.WhenAll(tasks);

        var pillarIndex = pillars.Select((p, i) => (p.Slug, i))
            .ToDictionary(x => x.Slug, x => x.i, StringComparer.OrdinalIgnoreCase);
        var ordered = validations
            .OrderBy(v => pillarIndex.GetValueOrDefault(v.Slug, int.MaxValue))
            .ToList();

        if (ordered.Count == 0)
            return ([], true, "No pillars to validate.");

        if (attempted > 0 && failures == attempted)
        {
            var reason = firstError ?? "SERP provider unavailable.";
            logger.LogInformation("SERP validation skipped: {Reason}", reason);
            return (ordered, true, reason);
        }

        return (ordered, false, null);
    }

    private static string AssessStrength(int serpPresence) => serpPresence switch
    {
        >= 4 => "dominant",
        >= 2 => "strong",
        _ => "moderate",
    };

    internal static string NormalizeHost(string domainOrUrl)
    {
        if (Uri.TryCreate(domainOrUrl, UriKind.Absolute, out var absolute))
            return StripWww(absolute.Host);

        if (Uri.TryCreate($"https://{domainOrUrl.Trim()}", UriKind.Absolute, out var withScheme))
            return StripWww(withScheme.Host);

        return StripWww(domainOrUrl.Trim());
    }

    internal static string? DomainFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;
        return StripWww(uri.Host);
    }

    private static string StripWww(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? host[4..].ToLowerInvariant()
            : host.ToLowerInvariant();

    private static bool IsSameSite(string domain, string siteHost) =>
        string.Equals(NormalizeHost(domain), NormalizeHost(siteHost), StringComparison.OrdinalIgnoreCase);
}

public sealed record PillarDemandEnrichment(
    IReadOnlyList<PillarKeywordEnrichment> Keywords,
    IReadOnlyList<PillarSerpEnrichment> SerpValidations,
    IReadOnlyList<NicheCompetitor> Competitors,
    IReadOnlyList<DiscoveredPillar> PillarsAfterDemotion,
    IReadOnlyList<string> DemotedSlugs,
    bool KeywordsSkipped,
    bool SerpSkipped,
    string? KeywordSkipReason,
    string? SerpSkipReason,
    string KeywordProvider,
    string SerpProvider);

public sealed record PillarKeywordEnrichment(
    string Slug,
    string Keyword,
    int SearchVolume,
    decimal KeywordDifficulty,
    bool Enriched,
    string? Error = null);

public sealed record PillarSerpEnrichment(
    string Slug,
    bool HasSerpFootprint,
    int OrganicResultCount,
    bool SiteRanks,
    int? SitePosition,
    IReadOnlyList<string> TopCompetitorDomains,
    string Provider,
    string? Error = null,
    IReadOnlyList<string>? ExpectedTopicSlugs = null,
    IReadOnlyList<PeopleAlsoAskResult>? PaaQuestions = null,
    IReadOnlyList<string>? RelatedSearches = null);
