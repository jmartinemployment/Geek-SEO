using System.Collections.Concurrent;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
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
    /// <summary>When running national + local SERP per pillar, keep below Serper's ~5 req/s cap.</summary>
    private const int DualSerpConcurrency = 1;
    private const int SerpRateLimitMaxAttempts = 4;
    private const int SerperInterRequestDelayMs = 350;

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
            serpProvider.ProviderName,
            serp.LocalStats);
    }

    public Task<(IReadOnlyList<PillarKeywordEnrichment> Enrichments, bool Skipped, string? SkipReason)>
        EnrichKeywordsOnlyAsync(
            IReadOnlyList<DiscoveredPillar> pillars,
            string location,
            Func<int, int, string, Task>? onProgress = null,
            CancellationToken ct = default) =>
        EnrichKeywordsAsync(pillars, location, onProgress, ct);

    public Task<SerpValidationPhaseResult> ValidateSerpOnlyAsync(
            IReadOnlyList<DiscoveredPillar> pillars,
            string siteDomain,
            string location,
            Func<int, int, string, Task>? onProgress = null,
            CancellationToken ct = default) =>
        ValidateSerpAsync(
            pillars,
            NormalizeHost(siteDomain),
            location,
            onProgress,
            ct);

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

        // national presence count
        var national = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        // local presence count
        var local = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rankingPillars = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var v in serpEnrichments)
        {
            foreach (var domain in v.TopCompetitorDomains)
            {
                if (string.IsNullOrWhiteSpace(domain) || IsSameSite(domain, siteHost)) continue;
                national[domain] = national.GetValueOrDefault(domain) + 1;
                TrackPillar(rankingPillars, domain, v.Slug);
            }

            foreach (var domain in v.LocalCompetitorDomains ?? [])
            {
                if (string.IsNullOrWhiteSpace(domain) || IsSameSite(domain, siteHost)) continue;
                local[domain] = local.GetValueOrDefault(domain) + 1;
                TrackPillar(rankingPillars, domain, v.Slug);
            }
        }

        var allDomains = national.Keys
            .Concat(local.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(d => (national.GetValueOrDefault(d) + local.GetValueOrDefault(d)))
            .ThenBy(d => d, StringComparer.OrdinalIgnoreCase);

        return allDomains.Select(domain =>
        {
            var inNational = national.ContainsKey(domain);
            var inLocal = local.ContainsKey(domain);
            var totalPresence = national.GetValueOrDefault(domain) + local.GetValueOrDefault(domain);
            var scope = (inNational, inLocal) switch
            {
                (true, true) => "both",
                (false, true) => "local",
                _ => "national",
            };

            return new NicheCompetitor
            {
                Id = Guid.NewGuid(),
                NicheProfileId = profileId,
                Domain = domain,
                SerpPresence = totalPresence,
                PillarsRanking = rankingPillars.GetValueOrDefault(domain)?.Count ?? 0,
                StrengthAssessment = AssessStrength(totalPresence),
                EstimatedAuthorityScore = Math.Min(totalPresence * 15m, 100m),
                Scope = scope,
            };
        }).ToList();
    }

    private static void TrackPillar(Dictionary<string, HashSet<string>> map, string domain, string slug)
    {
        if (!map.TryGetValue(domain, out var slugs))
        {
            slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            map[domain] = slugs;
        }
        slugs.Add(slug);
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
        SerpValidationPhaseResult Serp)>
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

    internal const string NationalLocation = "United States";

    private async Task<SerpValidationPhaseResult> ValidateSerpAsync(
            IReadOnlyList<DiscoveredPillar> pillars,
            string siteHost,
            string location,
            Func<int, int, string, Task>? onProgress,
            CancellationToken ct)
    {
        var isLocal = !string.IsNullOrWhiteSpace(location)
            && !location.Equals(NationalLocation, StringComparison.OrdinalIgnoreCase);

        var validations = new ConcurrentBag<PillarSerpEnrichment>();
        var failures = 0;
        var attempted = 0;
        var completed = 0;
        var localFailures = 0;
        var localSuccesses = 0;
        string? firstError = null;
        string? firstLocalError = null;
        var concurrency = isLocal ? DualSerpConcurrency : MaxConcurrency;
        using var gate = new SemaphoreSlim(concurrency);

        var tasks = pillars.Select(async pillar =>
        {
            await gate.WaitAsync(ct);
            try
            {
                Interlocked.Increment(ref attempted);
                var keyword = pillar.Name.Trim();

                var nationalRequest = new SerpRequest { Keyword = keyword, Location = NationalLocation, ResultCount = 10 };
                var nationalResult = await FetchSerpWithRetryAsync(nationalRequest, ct);

                Result<SerpResult>? localResult = null;
                if (isLocal)
                {
                    if (string.Equals(serpProvider.ProviderName, "serpdev", StringComparison.OrdinalIgnoreCase))
                        await Task.Delay(SerperInterRequestDelayMs, ct);

                    var localRequest = new SerpRequest { Keyword = keyword, Location = location, ResultCount = 10 };
                    localResult = await FetchSerpWithRetryAsync(localRequest, ct);
                    if (localResult.IsSuccess && localResult.Value is not null && HasLocalSerpSignal(localResult.Value))
                        Interlocked.Increment(ref localSuccesses);
                    else
                    {
                        Interlocked.Increment(ref localFailures);
                        firstLocalError ??= localResult.Error;
                    }
                }

                if (!nationalResult.IsSuccess || nationalResult.Value is null)
                {
                    Interlocked.Increment(ref failures);
                    firstError ??= nationalResult.Error;
                    validations.Add(new PillarSerpEnrichment(
                        pillar.Slug,
                        HasSerpFootprint: true,
                        OrganicResultCount: 0,
                        SiteRanks: false,
                        SitePosition: null,
                        TopCompetitorDomains: [],
                        serpProvider.ProviderName,
                        nationalResult.Error,
                        []));
                    return;
                }

                var organic = nationalResult.Value.OrganicResults;
                var expectedTopics = SerpEntityExtractor.ExtractTopicSlugs(nationalResult.Value);
                var hasFootprint = organic.Count > 0;
                int? sitePosition = null;
                foreach (var row in organic)
                {
                    var domain = DomainFromUrl(row.Url) ?? row.Domain;
                    if (domain is null || !IsSameSite(domain, siteHost)) continue;
                    sitePosition = row.Position;
                    break;
                }

                var nationalDomains = organic
                    .Select(r => DomainFromUrl(r.Url) ?? r.Domain)
                    .Where(d => !string.IsNullOrWhiteSpace(d) && CompetitorDomainFilter.IsCompetitor(d!))
                    .Select(d => d!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<string>? localDomains = null;
                IReadOnlyList<PeopleAlsoAskResult>? localPaa = null;
                IReadOnlyList<string>? localRelated = null;

                if (localResult?.IsSuccess == true && localResult.Value is not null)
                {
                    localDomains = CollectLocalCompetitorDomains(localResult.Value);
                    localPaa = localResult.Value.PeopleAlsoAsk.Count > 0 ? localResult.Value.PeopleAlsoAsk : null;
                    localRelated = localResult.Value.RelatedSearches.Count > 0 ? localResult.Value.RelatedSearches : null;
                }

                validations.Add(new PillarSerpEnrichment(
                    pillar.Slug,
                    hasFootprint,
                    organic.Count,
                    sitePosition.HasValue,
                    sitePosition,
                    nationalDomains,
                    serpProvider.ProviderName,
                    null,
                    expectedTopics,
                    nationalResult.Value.PeopleAlsoAsk.Count > 0 ? nationalResult.Value.PeopleAlsoAsk : null,
                    nationalResult.Value.RelatedSearches.Count > 0 ? nationalResult.Value.RelatedSearches : null,
                    localDomains,
                    localPaa,
                    localRelated));
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
            return new SerpValidationPhaseResult([], true, "No pillars to validate.", null);

        var localStats = SerpValidationMessages.BuildLocalStats(
            location, attempted, localSuccesses, localFailures, firstLocalError);

        if (attempted > 0 && failures == attempted)
        {
            var reason = firstError ?? "SERP provider unavailable.";
            logger.LogWarning("SERP validation skipped: {Reason}", reason);
            return new SerpValidationPhaseResult(ordered, true, reason, localStats);
        }

        if (isLocal)
        {
            if (localFailures > 0)
            {
                logger.LogWarning(
                    "Local SERP for {Location}: {Failures}/{Attempted} pillar queries failed; {Successes} succeeded. First error: {Error}",
                    location,
                    localFailures,
                    attempted,
                    localSuccesses,
                    firstLocalError ?? "(none)");
            }
            else
            {
                logger.LogInformation(
                    "Local SERP for {Location}: {Successes}/{Attempted} pillar queries succeeded",
                    location,
                    localSuccesses,
                    attempted);
            }
        }

        return new SerpValidationPhaseResult(ordered, false, null, localStats);
    }

    private async Task<Result<SerpResult>> FetchSerpWithRetryAsync(SerpRequest request, CancellationToken ct)
    {
        Result<SerpResult>? last = null;
        for (var attempt = 1; attempt <= SerpRateLimitMaxAttempts; attempt++)
        {
            last = await serpProvider.GetSerpResultsAsync(request, ct);
            if (last.IsSuccess && last.Value is not null)
                return last;

            if (attempt >= SerpRateLimitMaxAttempts || !IsRateLimited(last.Error))
                return last;

            var delaySeconds = Math.Pow(2, attempt - 1);
            logger.LogWarning(
                "SERP rate limited for {Keyword} @ {Location}; retry {Attempt}/{MaxAttempts} in {DelaySeconds}s",
                request.Keyword,
                request.Location,
                attempt,
                SerpRateLimitMaxAttempts,
                delaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
        }

        return last ?? Result<SerpResult>.Failure("SERP request failed.");
    }

    internal static bool IsRateLimited(string? error) =>
        !string.IsNullOrWhiteSpace(error)
        && (error.Contains("429", StringComparison.Ordinal)
            || error.Contains("rate limit", StringComparison.OrdinalIgnoreCase));

    private static bool HasLocalSerpSignal(SerpResult result) =>
        result.OrganicResults.Count > 0 || result.LocalPlaceDomains.Count > 0;

    private static List<string> CollectLocalCompetitorDomains(SerpResult result)
    {
        var domains = new List<string>();
        foreach (var row in result.OrganicResults)
        {
            var domain = DomainFromUrl(row.Url) ?? row.Domain;
            if (string.IsNullOrWhiteSpace(domain) || !CompetitorDomainFilter.IsCompetitor(domain))
                continue;
            if (!domains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                domains.Add(domain);
        }

        foreach (var domain in result.LocalPlaceDomains)
        {
            if (string.IsNullOrWhiteSpace(domain) || !CompetitorDomainFilter.IsCompetitor(domain))
                continue;
            if (!domains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                domains.Add(domain);
        }

        return domains;
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

public sealed record SerpValidationPhaseResult(
    IReadOnlyList<PillarSerpEnrichment> Validations,
    bool Skipped,
    string? SkipReason,
    SerpLocalQueryStats? LocalStats);

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
    string SerpProvider,
    SerpLocalQueryStats? LocalSerpStats = null);

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
    IReadOnlyList<string>? RelatedSearches = null,
    IReadOnlyList<string>? LocalCompetitorDomains = null,
    IReadOnlyList<PeopleAlsoAskResult>? LocalPaaQuestions = null,
    IReadOnlyList<string>? LocalRelatedSearches = null);
