using System.Text.Json;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Services.Seo;

public sealed class SerpResearchPackService(
    ISerpProvider serpProvider,
    ISerpCacheRepository serpCache,
    ICrawlerProvider crawler,
    CompetitorCrawlService competitorCrawl) : ISerpResearchPackService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<Result<SerpResearchPack>> BuildAsync(
        Guid userId,
        UrlAnalyzerResearchRequest request,
        CancellationToken ct = default)
    {
        _ = userId;
        var location = string.IsNullOrWhiteSpace(request.Location) ? "United States" : request.Location.Trim();
        var language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim();
        var notes = new List<string>();

        string keyword;
        string sourceUrl;
        string businessContext;
        IReadOnlyList<SerpResearchHeading> sourceHeadings;

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            keyword = request.Keyword.Trim();
            sourceUrl = string.IsNullOrWhiteSpace(request.Url)
                ? string.Empty
                : UrlPageKeywordResolver.NormalizeUrl(request.Url);
            businessContext = request.BusinessContext?.Trim() ?? string.Empty;
            sourceHeadings = [];
            notes.Add($"Search keyword from Site Analyzer: \"{keyword}\".");
        }
        else
        {
            sourceUrl = UrlPageKeywordResolver.NormalizeUrl(request.Url);
            if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out _))
                return Result<SerpResearchPack>.Failure("A valid url is required");

            var pageCrawl = await crawler.CrawlPageAsync(sourceUrl, ct);
            if (!pageCrawl.IsSuccess || pageCrawl.Value is null)
            {
                return Result<SerpResearchPack>.Failure(
                    pageCrawl.Error ?? "Could not crawl the URL to derive a search keyword.");
            }

            keyword = UrlPageKeywordResolver.Derive(pageCrawl.Value, sourceUrl);
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Result<SerpResearchPack>.Failure(
                    "Could not derive a search keyword from the page title, H1, or URL slug.");
            }

            notes.Add($"Search keyword derived from page: \"{keyword}\".");

            businessContext = UrlPageBusinessContextResolver.Derive(pageCrawl.Value, sourceUrl);
            if (!string.IsNullOrWhiteSpace(businessContext))
                notes.Add("Business context derived from source page for intent filtering only.");

            sourceHeadings = pageCrawl.Value.Headings
                .Where(h => h.Level is 2 or 3 && !string.IsNullOrWhiteSpace(h.Text))
                .Select(h => new SerpResearchHeading { Level = h.Level, Text = h.Text.Trim() })
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(keyword))
            return Result<SerpResearchPack>.Failure("A keyword is required");

        var serpRow = await EnsureSerpAsync(keyword, location, language, notes, ct);
        if (!serpRow.IsSuccess)
            return Result<SerpResearchPack>.Failure(serpRow.Error ?? "SERP fetch failed");

        if (serpRow.Value is null)
        {
            return Result<SerpResearchPack>.Failure(
                "Live SERP results are unavailable. Configure SERP_PROVIDER credentials.");
        }

        var serp = SerpResultStore.FromDbRow(serpRow.Value);
        if (serp is null || serp.OrganicResults.Count == 0)
        {
            return Result<SerpResearchPack>.Failure(
                "Live SERP returned no organic results for this keyword and location.");
        }

        var benchmarksPayload = JsonSerializer.Deserialize<SerpBenchmarksPayload>(serpRow.Value.ResultsJson, JsonOptions)
            ?? SerpBenchmarkCalculator.FromSerp(serp);

        var crawlResult = await competitorCrawl.EnsureCompetitorPagesAsync(
            serpRow.Value.Id,
            serp.OrganicResults,
            ct);

        var crawledPages = crawlResult.IsSuccess && crawlResult.Value is not null
            ? crawlResult.Value
            : [];

        if (!crawlResult.IsSuccess)
            notes.Add($"Competitor crawl incomplete: {crawlResult.Error}");

        var crawledTop5 = MatchCrawledPages(serp.OrganicResults, crawledPages, 5);
        if (crawledTop5.Count < 3)
            notes.Add("Fewer than 3 competitor pages crawled; word-count benchmark uses estimates.");

        var refinedBenchmarks = CompetitorCrawlService.BenchmarksFromCompetitors(serp, crawledPages, benchmarksPayload);
        var dataQuality = ResolveDataQuality(serp, crawledTop5, notes);

        var organic = serp.OrganicResults.Take(10).Select(MapOrganic).ToList();
        var competitorOutlines = BuildCompetitorOutlines(serp.OrganicResults, crawledPages);
        var recommendedTerms = BuildRecommendedTerms(keyword, serp);
        var closingFaq = BuildClosingFaq(keyword, serp);
        var intent = InferIntent(keyword, serp, organic, businessContext);
        var paf = BuildPaf(serp);
        var serpFeatures = BuildSerpFeatureList(serp);
        var medianWordCount = MedianWordCountTop5(crawledTop5, refinedBenchmarks.AvgWordCount);
        var medianTitleLength = MedianTitleLength(serp.OrganicResults.Take(10).ToList());
        var dominantFormat = InferDominantFormat(organic, crawledTop5);

        return Result<SerpResearchPack>.Success(new SerpResearchPack
        {
            Meta = new SerpResearchPackMeta
            {
                SourceUrl = sourceUrl,
                Keyword = keyword,
                Location = location,
                Language = language,
                ResearchedAt = DateTimeOffset.UtcNow.ToString("O"),
                DataQuality = dataQuality,
                BusinessContext = businessContext,
                Notes = notes,
            },
            Intent = intent,
            Paf = paf,
            Paa = serp.PeopleAlsoAsk.Select(p => new SerpResearchPaaItem
            {
                Question = p.Question,
                SerpAnswerPreview = p.Answer ?? "",
                Depth = 1,
            }).ToList(),
            Pasf = serp.RelatedSearches.ToList(),
            SerpFeatures = serpFeatures,
            Organic = organic,
            CompetitorOutlines = competitorOutlines,
            SourceHeadings = sourceHeadings,
            Benchmarks = new SerpResearchBenchmarks
            {
                MedianWordCountTop5 = medianWordCount,
                MedianTitleLengthTop10 = medianTitleLength,
                DominantContentFormat = dominantFormat,
            },
            RecommendedTerms = recommendedTerms,
            ClosingFaqQuestions = closingFaq,
            DirectAnswerBlock = BuildDirectAnswerBlock(keyword, paf),
            MethodologyHints = BuildMethodologyHints(keyword, competitorOutlines, serp),
        });
    }

    private async Task<Result<SeoSerpResult?>> EnsureSerpAsync(
        string keyword,
        string location,
        string language,
        List<string> notes,
        CancellationToken ct)
    {
        var cache = await serpCache.GetAsync(keyword, location, language, ct);
        if (cache.IsSuccess && cache.Value is not null && cache.Value.ExpiresAt > DateTimeOffset.UtcNow)
            return Result<SeoSerpResult?>.Success(cache.Value);

        var fetch = await serpProvider.GetSerpResultsAsync(new SerpRequest
        {
            Keyword = keyword,
            Location = location,
            LanguageCode = language,
            ResultCount = 10,
        }, ct);

        if (!fetch.IsSuccess || fetch.Value is null)
        {
            notes.Add(fetch.Error ?? "SERP provider returned no data");
            return Result<SeoSerpResult?>.Failure(fetch.Error ?? "SERP fetch failed");
        }

        var benchmarks = SerpBenchmarkCalculator.FromSerp(fetch.Value);
        var upserted = await serpCache.UpsertAsync(keyword, location, language, fetch.Value, benchmarks, ct);
        if (upserted.IsSuccess && upserted.Value is not null)
            return Result<SeoSerpResult?>.Success(upserted.Value);

        notes.Add("SERP cache write failed; using ephemeral live result.");
        return Result<SeoSerpResult?>.Success(
            SerpResultStore.ToEphemeralRow(fetch.Value, benchmarks, language, retentionDays: 90));
    }

    private static string ResolveDataQuality(SerpResult serp, IReadOnlyList<SeoCompetitorPage> crawledTop5, List<string> notes)
    {
        if (serp.OrganicResults.Count == 0)
            return "unavailable";

        if (crawledTop5.Count >= 3)
            return "live";

        if (serp.OrganicResults.Count >= 5)
        {
            notes.Add("SERP organic results present but competitor page depth is limited.");
            return "partial";
        }

        return "partial";
    }

    private static SerpResearchOrganicItem MapOrganic(SerpOrganicResult row) => new()
    {
        Position = row.Position,
        Url = row.Url,
        Domain = row.Domain ?? ExtractDomain(row.Url),
        Title = row.Title,
        Snippet = row.Snippet,
        ContentType = InferContentType(row),
    };

    private static string InferContentType(SerpOrganicResult row)
    {
        var haystack = $"{row.Url} {row.Title}".ToLowerInvariant();
        if (haystack.Contains("reddit.com") || haystack.Contains("/forum"))
            return "forum";
        if (haystack.Contains("/product") || haystack.Contains("/shop") || haystack.Contains("/pricing"))
            return "product";
        if (haystack.Contains("/service") || haystack.Contains("services/"))
            return "service";
        if (haystack.Contains("how-to") || haystack.Contains("how to") || haystack.Contains("guide"))
            return "guide";
        return "other";
    }

    private static List<SeoCompetitorPage> MatchCrawledPages(
        IReadOnlyList<SerpOrganicResult> organic,
        IReadOnlyList<SeoCompetitorPage> crawled,
        int take)
    {
        var results = new List<SeoCompetitorPage>();
        foreach (var row in organic.Take(take))
        {
            var match = crawled.FirstOrDefault(c =>
                string.Equals(c.Url, row.Url, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                results.Add(match);
        }

        return results;
    }

    private static IReadOnlyList<SerpResearchCompetitorOutline> BuildCompetitorOutlines(
        IReadOnlyList<SerpOrganicResult> organic,
        IReadOnlyList<SeoCompetitorPage> crawled)
    {
        return organic.Take(5).Select(row =>
        {
            var page = crawled.FirstOrDefault(c =>
                string.Equals(c.Url, row.Url, StringComparison.OrdinalIgnoreCase));
            return MapCompetitorOutline(row, page);
        }).ToList();
    }

    private static SerpResearchCompetitorOutline MapCompetitorOutline(
        SerpOrganicResult organic,
        SeoCompetitorPage? page)
    {
        var headings = ParseHeadings(page?.HeadingsJson);
        var h1 = headings.FirstOrDefault(h => h.Level == 1)?.Text
            ?? page?.MetaTitle
            ?? organic.Title;

        var schemaTypes = ParseSchemaTypes(page?.StructuredDataTypesJson);
        var wordCount = page?.WordCount > 0
            ? page.WordCount
            : Math.Max(800, CountWords(organic.Snippet) * 12);

        return new SerpResearchCompetitorOutline
        {
            Url = organic.Url,
            Position = organic.Position,
            H1 = h1 ?? "",
            Headings = headings.Where(h => h.Level is 2 or 3).ToList(),
            EstimatedWordCount = wordCount,
            SchemaTypes = schemaTypes,
        };
    }

    private static IReadOnlyList<SerpResearchHeading> ParseHeadings(string? headingsJson)
    {
        if (string.IsNullOrWhiteSpace(headingsJson))
            return [];

        try
        {
            var structured = JsonSerializer.Deserialize<List<SerpResearchHeading>>(headingsJson, JsonOptions);
            if (structured is { Count: > 0 })
                return structured;

            var flat = JsonSerializer.Deserialize<List<string>>(headingsJson, JsonOptions) ?? [];
            return flat.Select((text, index) => new SerpResearchHeading
            {
                Level = index == 0 ? 1 : 2,
                Text = text,
            }).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ParseSchemaTypes(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(schemaJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> BuildRecommendedTerms(string keyword, SerpResult serp)
    {
        var organic = serp.OrganicResults.Take(10).Select(o => new DeepSerpOrganic
        {
            Position = o.Position,
            Url = o.Url,
            Title = o.Title,
            Snippet = o.Snippet,
            Domain = o.Domain,
        }).ToList();

        var matrix = SerpTermMatrixBuilder.Build(organic);
        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            var trimmed = value.Trim();
            if (seen.Add(trimmed))
                terms.Add(trimmed);
        }

        foreach (var term in matrix.Terms.Take(8))
            Add(term);

        foreach (var related in serp.RelatedSearches)
        {
            Add(related);
            if (terms.Count >= 12)
                break;
        }

        foreach (var token in keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            Add(token);
            if (terms.Count >= 12)
                break;
        }

        return terms.Take(12).ToList();
    }

    private static IReadOnlyList<SerpResearchClosingFaqItem> BuildClosingFaq(string keyword, SerpResult serp)
    {
        var paaQuestions = serp.PeopleAlsoAsk.Select(p => p.Question).Where(q => q.Length > 0).ToList();
        var pasf = serp.RelatedSearches.ToList();
        var items = new List<SerpResearchClosingFaqItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string question, string source)
        {
            var trimmed = question.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || !seen.Add(trimmed))
                return;
            items.Add(new SerpResearchClosingFaqItem { Question = trimmed, Source = source });
        }

        foreach (var question in SerpQuestionFilter.Filter(paaQuestions))
        {
            Add(question, "paa");
            if (items.Count >= ContentWritingRules.ClosingFaqCount)
                return items;
        }

        foreach (var fallback in ContentWritingRules.BuildClosingFaqQuestions(keyword, [], null))
        {
            Add(fallback, "suggested");
            if (items.Count >= ContentWritingRules.ClosingFaqCount)
                break;
        }

        return items.Take(ContentWritingRules.ClosingFaqCount).ToList();
    }

    private static SerpResearchIntent InferIntent(
        string keyword,
        SerpResult serp,
        IReadOnlyList<SerpResearchOrganicItem> organic,
        string businessContext)
    {
        var lower = keyword.ToLowerInvariant();
        var contextLower = businessContext.ToLowerInvariant();
        var serviceCount = organic.Count(o => o.ContentType is "service" or "product");
        var guideCount = organic.Count(o => o.ContentType is "guide" or "other");
        var forumCount = organic.Count(o => o.ContentType is "forum");

        string primary;
        string justification;

        if (serp.Features.HasLocalPack || lower.Contains("near me", StringComparison.Ordinal)
            || contextLower.Contains("service area", StringComparison.Ordinal)
            || contextLower.Contains("locally", StringComparison.Ordinal))
        {
            primary = "local";
            justification = "SERP includes local pack signals, near-me query language, or local business page context.";
        }
        else if (lower.Contains("buy", StringComparison.Ordinal) || lower.Contains("price", StringComparison.Ordinal)
            || lower.Contains("cost", StringComparison.Ordinal) || serviceCount >= 4
            || contextLower.Contains("pricing", StringComparison.Ordinal)
            || contextLower.Contains("consulting", StringComparison.Ordinal))
        {
            primary = "commercial";
            justification = "Query, SERP mix, or page context skews toward pricing, services, or purchase evaluation.";
        }
        else if (lower.Contains("hire", StringComparison.Ordinal) || lower.Contains("book", StringComparison.Ordinal))
        {
            primary = "transactional";
            justification = "Query language indicates ready-to-act transactional intent.";
        }
        else if (guideCount >= 5 || lower.Contains("how ", StringComparison.Ordinal) || lower.Contains("what is", StringComparison.Ordinal))
        {
            primary = "informational";
            justification = "SERP is dominated by guides and definitional content.";
        }
        else if (forumCount >= 3)
        {
            primary = "informational";
            justification = "Forum-heavy SERP suggests research and comparison intent.";
        }
        else
        {
            primary = "informational";
            justification = "Default informational classification from organic title/snippet mix.";
        }

        return new SerpResearchIntent { Primary = primary, Justification = justification };
    }

    private static SerpResearchPaf BuildPaf(SerpResult serp)
    {
        if (serp.Features.HasAiOverview)
        {
            return new SerpResearchPaf
            {
                Type = "ai_overview",
                Format = "mixed",
                Text = SerpCaptureTextSanitizer.Sanitize(serp.FeaturedSnippetText) ?? "",
                BeatStrategy = string.Empty,
            };
        }

        if (serp.Features.HasKnowledgePanel)
        {
            return new SerpResearchPaf
            {
                Type = "knowledge_panel",
                Format = "paragraph",
                Text = serp.FeaturedSnippetText ?? "",
                BeatStrategy = "Expand beyond entity facts with actionable implementation detail.",
            };
        }

        if (serp.Features.HasFeaturedSnippet && !string.IsNullOrWhiteSpace(serp.FeaturedSnippetText))
        {
            var snippetText = SerpCaptureTextSanitizer.Sanitize(serp.FeaturedSnippetText);
            if (!string.IsNullOrWhiteSpace(snippetText))
            {
                return new SerpResearchPaf
                {
                    Type = "featured_snippet",
                    Format = InferPafFormat(snippetText),
                    Text = snippetText,
                    BeatStrategy = "Match snippet format but add specificity, examples, and updated facts.",
                };
            }
        }

        return new SerpResearchPaf
        {
            Type = "none",
            Format = "paragraph",
            Text = "",
            BeatStrategy = "Open with a concise definition and outcome in the first 2–3 sentences.",
        };
    }

    private static string InferPafFormat(string text)
    {
        if (text.Contains('\n') || text.Contains("•") || text.Contains("- "))
            return "list";
        if (text.Contains("|") || text.Split('\t').Length > 2)
            return "table";
        return "paragraph";
    }

    private static IReadOnlyList<string> BuildSerpFeatureList(SerpResult serp)
    {
        var features = new List<string>();
        if (serp.Features.HasLocalPack) features.Add("local_pack");
        if (serp.Features.HasVideoCarousel) features.Add("videos");
        if (serp.Features.HasPeopleAlsoAsk) features.Add("faq_rich_results");
        if (serp.Features.HasFeaturedSnippet) features.Add("featured_snippet");
        if (serp.Features.HasAiOverview) features.Add("ai_overview");
        if (serp.Features.HasKnowledgePanel) features.Add("knowledge_panel");
        if (serp.OrganicResults.Any(o => o.Domain?.Contains("reddit.com", StringComparison.OrdinalIgnoreCase) == true))
            features.Add("forums");
        return features;
    }

    private static int MedianWordCountTop5(IReadOnlyList<SeoCompetitorPage> crawledTop5, int fallback)
    {
        var counts = crawledTop5
            .Where(p => p.WordCount > 0)
            .Select(p => p.WordCount)
            .OrderBy(v => v)
            .ToList();

        if (counts.Count == 0)
            return fallback;

        var mid = counts.Count / 2;
        return counts.Count % 2 == 1
            ? counts[mid]
            : (counts[mid - 1] + counts[mid]) / 2;
    }

    private static int MedianTitleLength(IReadOnlyList<SerpOrganicResult> organic)
    {
        var lengths = organic
            .Select(o => o.Title.Length)
            .Where(len => len > 0)
            .OrderBy(len => len)
            .ToList();

        if (lengths.Count == 0)
            return 55;

        var mid = lengths.Count / 2;
        return lengths.Count % 2 == 1
            ? lengths[mid]
            : (lengths[mid - 1] + lengths[mid]) / 2;
    }

    private static string InferDominantFormat(
        IReadOnlyList<SerpResearchOrganicItem> organic,
        IReadOnlyList<SeoCompetitorPage> crawledTop5)
    {
        var guideCount = organic.Count(o => o.ContentType == "guide");
        var serviceCount = organic.Count(o => o.ContentType is "service" or "product");
        var avgWords = crawledTop5.Where(p => p.WordCount > 0).Select(p => p.WordCount).DefaultIfEmpty(0).Average();

        if (serviceCount >= 4)
            return "local_service";
        if (guideCount >= 4 && avgWords >= 1800)
            return "long_guide";
        if (guideCount >= 3)
            return "how_to";
        if (organic.Count(o => o.Title.Contains("vs", StringComparison.OrdinalIgnoreCase)) >= 2)
            return "comparison";
        return "mixed";
    }

    private static SerpResearchDirectAnswerBlock BuildDirectAnswerBlock(string keyword, SerpResearchPaf paf)
    {
        var mustBeat = paf.Type is not "none";
        var instruction = string.Equals(paf.Type, "ai_overview", StringComparison.OrdinalIgnoreCase)
            ? SerpFeatureGuidanceBuilder.BuildAiOverviewDraftInstruction(keyword)
            : mustBeat
                ? $"Open with a direct answer to \"{keyword}\" in 2–3 sentences: definition, who it helps, and the primary outcome. Match or beat the {paf.Type.Replace('_', ' ')} format ({paf.Format})."
                : $"Open with a direct answer to \"{keyword}\" in 2–3 sentences: definition, who it helps, and the primary outcome.";

        return new SerpResearchDirectAnswerBlock
        {
            Instruction = instruction,
            MustBeatPaf = mustBeat,
        };
    }

    private static IReadOnlyList<SerpResearchMethodologyHint> BuildMethodologyHints(
        string keyword,
        IReadOnlyList<SerpResearchCompetitorOutline> outlines,
        SerpResult serp)
    {
        var h2Pool = outlines
            .SelectMany(o => o.Headings.Where(h => h.Level == 2).Select(h => h.Text))
            .Where(text => text.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var paaPool = serp.PeopleAlsoAsk
            .Select(p => p.Question)
            .Where(q => q.Length > 0)
            .ToList();

        var plans = MethodologySectionHintBuilder.BuildPlans(keyword, h2Pool.Concat(paaPool));
        return plans
            .Select((plan, index) => new SerpResearchMethodologyHint
            {
                Movement = index + 1,
                Label = plan.Phase.Label,
                SuggestedH2 = plan.SuggestedH2,
                SubtopicsFromSerp = plan.SubtopicsFromSerp,
            })
            .ToList();
    }

    private static string ExtractDomain(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : url;
    }

    private static int CountWords(string text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
