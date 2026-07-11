using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

public sealed class OperatorResearchEnricher(ISerpProvider serpProvider)
{
    private const int MaxParallelQueries = 3;
    private const int MaxCitationsPerBucket = 2;
    private const int MaxTotalCitations = 10;

    public async Task<WritingResearchContext> EnrichContextAsync(
        WritingResearchContext context,
        IReadOnlyList<ContentWriterManualResearchLane>? manualLanes = null,
        CancellationToken ct = default)
    {
        var keyword = string.IsNullOrWhiteSpace(context.DerivedKeyword)
            ? context.SerpKeyword
            : context.DerivedKeyword.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return context;

        var options = BuildOptions(context, keyword);
        var templates = FilterTemplates(OperatorResearchQueryPack.Build(options), manualLanes ?? []);
        if (templates.Count == 0)
            return context;

        var results = await RunQueriesAsync(templates, context.SearchLocation, ct);
        return ApplyResults(context, templates, results);
    }

    public async Task<ContentWriterSerpExport> EnrichExportAsync(
        ContentWriterSerpExport export,
        string articleKeyword,
        string searchLocation,
        CancellationToken ct = default)
    {
        var keyword = string.IsNullOrWhiteSpace(articleKeyword) ? export.Keyword : articleKeyword.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
            return export;

        var options = new OperatorResearchQueryOptions
        {
            Keyword = keyword,
            TargetSiteUrl = export.TargetSiteUrl,
            LocalCity = ResolveLocalCity(searchLocation, null),
        };
        var templates = FilterTemplates(OperatorResearchQueryPack.Build(options), export.ManualResearchLanes);
        if (templates.Count == 0)
            return export;

        var results = await RunQueriesAsync(templates, searchLocation, ct);
        return ApplyExportResults(export, templates, results);
    }

    private static IReadOnlyList<OperatorResearchQueryTemplate> FilterTemplates(
        IReadOnlyList<OperatorResearchQueryTemplate> templates,
        IReadOnlyList<ContentWriterManualResearchLane> manualLanes) =>
        manualLanes.Count == 0
            ? templates
            : templates
                .Where(t => !ManualResearchLaneMerger.HasNonEmptyManualLane(manualLanes, t.Bucket))
                .ToList();

    private static OperatorResearchQueryOptions BuildOptions(WritingResearchContext context, string keyword) =>
        new()
        {
            Keyword = keyword,
            TargetSiteUrl = context.SourceUrl,
            LocalCity = ResolveLocalCity(context.SearchLocation, context.SiteFocus),
        };

    private static string ResolveLocalCity(string searchLocation, SiteWritingFocus? focus) =>
        OperatorResearchLocalCity.Resolve(searchLocation, focus);

    private async Task<IReadOnlyList<OperatorResearchQueryResult>> RunQueriesAsync(
        IReadOnlyList<OperatorResearchQueryTemplate> templates,
        string searchLocation,
        CancellationToken ct)
    {
        using var gate = new SemaphoreSlim(MaxParallelQueries);
        var tasks = templates.Select(async template =>
        {
            await gate.WaitAsync(ct);
            try
            {
                if (string.Equals(template.SearchEngine, "google_scholar", StringComparison.OrdinalIgnoreCase)
                    && !SupportsScholar(serpProvider.ProviderName))
                {
                    return new OperatorResearchQueryResult(template, null, "Scholar not supported by SERP provider.");
                }

                var fetch = await serpProvider.GetSerpResultsAsync(new SerpRequest
                {
                    Keyword = template.Query,
                    Location = string.IsNullOrWhiteSpace(searchLocation) ? "United States" : searchLocation,
                    ResultCount = 5,
                    Engine = template.SearchEngine,
                }, ct);

                return new OperatorResearchQueryResult(
                    template,
                    fetch.IsSuccess ? fetch.Value : null,
                    fetch.IsSuccess ? null : fetch.Error);
            }
            finally
            {
                gate.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    private static bool SupportsScholar(string providerName) =>
        providerName.Contains("serpapi", StringComparison.OrdinalIgnoreCase);

    private static WritingResearchContext ApplyResults(
        WritingResearchContext context,
        IReadOnlyList<OperatorResearchQueryTemplate> templates,
        IReadOnlyList<OperatorResearchQueryResult> results)
    {
        var citations = BuildCitationCandidates(results);
        var supplementalPaa = CollectSupplementalQuestions(results);
        var featuredSnippet = PickFeaturedSnippet(results);
        var newsHooks = CollectNewsHooks(results);
        var localAngle = BuildLocalAngleHint(results);
        var ownSitePages = CollectOwnSitePages(results, context.SourceUrl);

        var mergedPaa = MergeQuestions(
            context.PeopleAlsoAsk.Select(p => p.Question),
            supplementalPaa);

        var mergedCitations = MergeCitations(context.CitationCandidates, citations);

        return context with
        {
            CitationCandidates = mergedCitations,
            PeopleAlsoAsk = mergedPaa,
            FeaturedSnippetResearch = featuredSnippet ?? context.FeaturedSnippetResearch,
            NewsHooks = newsHooks,
            LocalAngleHint = localAngle,
            OwnSiteLinkCandidates = ownSitePages,
            OperatorQueries = templates
                .Select(t => new WritingResearchOperatorQuery
                {
                    Bucket = t.Bucket,
                    Label = t.Label,
                    Query = t.Query,
                    SearchEngine = t.SearchEngine,
                })
                .ToList(),
            DirectAnswerInstruction = BuildDirectAnswerInstruction(
                context.DerivedKeyword,
                featuredSnippet,
                context.DirectAnswerInstruction),
        };
    }

    private static ContentWriterSerpExport ApplyExportResults(
        ContentWriterSerpExport export,
        IReadOnlyList<OperatorResearchQueryTemplate> templates,
        IReadOnlyList<OperatorResearchQueryResult> results)
    {
        var citations = BuildCitationCandidates(results)
            .Select(c => new ContentWriterCitationCandidate
            {
                Url = c.Url,
                Title = c.Title,
                Domain = c.Domain,
                Source = c.Source,
            })
            .ToList();

        var merged = MergeExportCitations(export.CitationCandidates, citations);

        return export with
        {
            CitationCandidates = merged,
            OperatorQueries = templates
                .Select(t => new ContentWriterOperatorQuery
                {
                    Bucket = t.Bucket,
                    Label = t.Label,
                    Query = t.Query,
                    SearchEngine = t.SearchEngine,
                })
                .ToList(),
            SupplementalPaaQuestions = CollectSupplementalQuestions(results),
            FeaturedSnippetCandidate = PickFeaturedSnippet(results),
            NewsHooks = CollectNewsHooks(results),
            LocalAngleHint = BuildLocalAngleHint(results),
        };
    }

    private static IReadOnlyList<WritingResearchCitationCandidate> BuildCitationCandidates(
        IReadOnlyList<OperatorResearchQueryResult> results)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var citations = new List<WritingResearchCitationCandidate>();

        foreach (var result in results.Where(r => r.Template.Bucket.StartsWith("citations_", StringComparison.Ordinal)
                                                  || r.Template.Bucket == "scholar"))
        {
            if (result.Serp is null)
                continue;

            var source = MapCitationSource(result.Template.Bucket);
            foreach (var organic in result.Serp.OrganicResults.Take(MaxCitationsPerBucket))
            {
                if (citations.Count >= MaxTotalCitations)
                    break;

                if (!IsAcceptableCitation(organic.Url, result.Template.Bucket))
                    continue;

                var normalized = organic.Url.Trim();
                if (!seen.Add(normalized))
                    continue;

                citations.Add(new WritingResearchCitationCandidate
                {
                    Url = normalized,
                    Title = organic.Title ?? string.Empty,
                    Domain = organic.Domain ?? string.Empty,
                    Source = source,
                });
            }
        }

        return citations;
    }

    private static bool IsAcceptableCitation(string url, string bucket)
    {
        if (bucket == "scholar")
            return AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(url)
                   || Uri.TryCreate(url, UriKind.Absolute, out _);

        return AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(url);
    }

    private static string MapCitationSource(string bucket) => bucket switch
    {
        "citations_wikipedia" => "wikipedia",
        "citations_government" => "government",
        "citations_research" => "research",
        "citations_pdf" => "research_pdf",
        "scholar" => "scholar",
        _ => "research",
    };

    private static IReadOnlyList<string> CollectSupplementalQuestions(IReadOnlyList<OperatorResearchQueryResult> results)
    {
        var questions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var result in results.Where(r => r.Template.Bucket == "paa_supplement"))
        {
            if (result.Serp is null)
                continue;

            foreach (var question in result.Serp.PeopleAlsoAsk.Select(p => p.Question))
                AddQuestion(questions, seen, question);

            foreach (var organic in result.Serp.OrganicResults)
            {
                if (!string.IsNullOrWhiteSpace(organic.Title) && organic.Title.Contains('?'))
                    AddQuestion(questions, seen, organic.Title);
            }
        }

        return SerpQuestionFilter.Filter(questions).Take(8).ToList();
    }

    private static void AddQuestion(List<string> questions, HashSet<string> seen, string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return;

        var trimmed = question.Trim();
        if (!seen.Add(trimmed))
            return;

        questions.Add(trimmed);
    }

    private static string? PickFeaturedSnippet(IReadOnlyList<OperatorResearchQueryResult> results)
    {
        foreach (var result in results.Where(r => r.Template.Bucket.StartsWith("featured_snippet", StringComparison.Ordinal)))
        {
            var text = result.Serp?.FeaturedSnippetText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return SerpCaptureTextSanitizer.Sanitize(text);
        }

        return null;
    }

    private static IReadOnlyList<string> CollectNewsHooks(IReadOnlyList<OperatorResearchQueryResult> results)
    {
        var hooks = new List<string>();
        foreach (var result in results.Where(r => r.Template.Bucket == "news"))
        {
            if (result.Serp is null)
                continue;

            foreach (var organic in result.Serp.OrganicResults.Take(3))
            {
                if (string.IsNullOrWhiteSpace(organic.Title))
                    continue;

                hooks.Add(organic.Title.Trim());
            }
        }

        return hooks;
    }

    private static string? BuildLocalAngleHint(IReadOnlyList<OperatorResearchQueryResult> results)
    {
        var snippets = results
            .Where(r => r.Template.Bucket == "local_angle")
            .SelectMany(r => r.Serp?.OrganicResults ?? [])
            .Select(o => o.Snippet)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(2)
            .ToList();

        return snippets.Count == 0
            ? null
            : string.Join(" ", snippets).Trim();
    }

    private static IReadOnlyList<WritingResearchOwnSiteLink> CollectOwnSitePages(
        IReadOnlyList<OperatorResearchQueryResult> results,
        string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var targetUri))
            return [];

        var targetHost = NormalizeHost(targetUri.Host);
        var pages = new List<WritingResearchOwnSiteLink>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var result in results.Where(r => r.Template.Bucket == "own_site"))
        {
            foreach (var organic in result.Serp?.OrganicResults ?? [])
            {
                if (!Uri.TryCreate(organic.Url, UriKind.Absolute, out var uri))
                    continue;

                if (!string.Equals(NormalizeHost(uri.Host), targetHost, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!seen.Add(organic.Url.Trim()))
                    continue;

                pages.Add(new WritingResearchOwnSiteLink
                {
                    Url = organic.Url.Trim(),
                    Title = organic.Title ?? string.Empty,
                });
            }
        }

        return pages;
    }

    private static string NormalizeHost(string host) =>
        host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;

    private static IReadOnlyList<WritingResearchPaa> MergeQuestions(
        IEnumerable<string> existing,
        IReadOnlyList<string> supplemental)
    {
        var merged = new List<WritingResearchPaa>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 1;

        foreach (var question in existing.Concat(supplemental))
        {
            if (string.IsNullOrWhiteSpace(question) || !seen.Add(question.Trim()))
                continue;

            if (SerpQuestionFilter.IsBlocked(question))
                continue;

            merged.Add(new WritingResearchPaa
            {
                Question = question.Trim(),
                DisplayOrder = order++,
            });
        }

        return merged;
    }

    private static IReadOnlyList<WritingResearchCitationCandidate> MergeCitations(
        IReadOnlyList<WritingResearchCitationCandidate> existing,
        IReadOnlyList<WritingResearchCitationCandidate> enriched)
    {
        var merged = new List<WritingResearchCitationCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in enriched.Concat(existing))
        {
            if (string.IsNullOrWhiteSpace(candidate.Url))
                continue;

            if (string.Equals(candidate.Source, "organic", StringComparison.OrdinalIgnoreCase))
                continue;

            var url = candidate.Url.Trim();
            if (!AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(url)
                && !AuthoritativeCitationRules.IsAuthoritativeCitationUrl(url))
                continue;

            if (!seen.Add(url))
                continue;

            merged.Add(candidate);
        }

        return merged.Take(MaxTotalCitations).ToList();
    }

    private static IReadOnlyList<ContentWriterCitationCandidate> MergeExportCitations(
        IReadOnlyList<ContentWriterCitationCandidate> existing,
        IReadOnlyList<ContentWriterCitationCandidate> enriched)
    {
        var merged = new List<ContentWriterCitationCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in enriched.Concat(existing))
        {
            if (string.Equals(candidate.Source, "organic", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!seen.Add(candidate.Url.Trim()))
                continue;

            if (!AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(candidate.Url)
                && !AuthoritativeCitationRules.IsAuthoritativeCitationUrl(candidate.Url))
                continue;

            merged.Add(candidate);
        }

        return merged.Take(MaxTotalCitations).ToList();
    }

    private static string BuildDirectAnswerInstruction(
        string keyword,
        string? featuredSnippet,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(featuredSnippet))
            return fallback;

        var sanitized = SerpCaptureTextSanitizer.Sanitize(featuredSnippet);
        if (string.IsNullOrWhiteSpace(sanitized))
            return fallback;

        return
            $"Open with a direct 40–60 word answer to \"what is {keyword}\". Outperform this featured snippet: {sanitized}";
    }

    private sealed record OperatorResearchQueryResult(
        OperatorResearchQueryTemplate Template,
        SerpResult? Serp,
        string? Error);
}
