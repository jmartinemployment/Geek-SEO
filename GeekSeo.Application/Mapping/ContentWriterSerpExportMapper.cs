using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeo.Application.Mapping;

public static class ContentWriterSerpExportMapper
{
    public static WritingResearchContext ToWritingResearchContext(
        ContentWriterSerpExport export,
        Guid userId,
        string searchLocation = "United States",
        string? articleKeyword = null)
    {
        var serpKeyword = export.Keyword;
        var derivedKeyword = string.IsNullOrWhiteSpace(articleKeyword) ? serpKeyword : articleKeyword.Trim();
        var organicItems = export.Serp
            .Where(i => string.Equals(i.Type, "organic", StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Position)
            .ToList();

        var pasfQueries = CollectPasfQueries(export.Serp);
        var paaItems = CollectPaaItems(export.Serp);

        var organic = organicItems
            .Select(MapOrganic)
            .ToList();

        var competitors = export.Competitors.Count > 0
            ? MapCrawledCompetitors(export.Competitors)
            : organicItems
                .Take(5)
                .Select(item => new WritingResearchCompetitor
                {
                    Url = item.Url ?? string.Empty,
                    Position = item.Position,
                    H1 = item.Title ?? string.Empty,
                    EstimatedWordCount = EstimateWordCount(item.Snippet),
                    Headings = [],
                })
                .ToList();

        var sourceHeadings = export.SourceHeadings
            .OrderBy(h => h.Sequence)
            .Select((h, index) => new WritingResearchHeading
            {
                Level = h.Level,
                Text = h.Text,
                DisplayOrder = h.Sequence > 0 ? h.Sequence : index + 1,
            })
            .ToList();

        var aiOverview = export.Serp
            .FirstOrDefault(i => string.Equals(i.Type, "ai_overview", StringComparison.OrdinalIgnoreCase));

        var recommendedTerms = BuildRecommendedTerms(
            derivedKeyword,
            pasfQueries,
            paaItems.Select(p => p.Question).ToList(),
            organicItems);
        var closingFaqs = BuildClosingFaqs(derivedKeyword, paaItems, pasfQueries);
        var sectionHints = BuildSectionHints(
            derivedKeyword,
            organicItems,
            paaItems.Select(p => p.Question).ToList(),
            pasfQueries);
        var paf = BuildPaf(aiOverview);
        var benchmarks = BuildBenchmarks(organicItems, export.Benchmarks);
        var intent = InferIntent(derivedKeyword, organic);
        var researchedAt = export.CapturedAt != default
            ? export.CapturedAt
            : export.SerpCapturedAt ?? DateTimeOffset.UtcNow;

        return new WritingResearchContext
        {
            AnalysisRunId = export.RunId,
            ProjectId = export.ProjectId,
            UserId = userId,
            SourceUrl = export.TargetSiteUrl,
            DerivedKeyword = derivedKeyword,
            SerpKeyword = serpKeyword,
            SearchLocation = searchLocation,
            IntentPrimary = intent.Primary,
            IntentJustification = intent.Justification,
            Paf = paf,
            DirectAnswerInstruction = BuildDirectAnswerInstruction(derivedKeyword, paf),
            MustBeatPaf = paf.Type is not "none",
            Benchmarks = benchmarks,
            DataQuality = organic.Count >= 3 ? "live" : organic.Count > 0 ? "partial" : "unavailable",
            DataQualityNotes = organic.Count == 0
                ? "SERP export has no organic results."
                : null,
            ResearchedAt = researchedAt,
            Organic = organic,
            PeopleAlsoAsk = paaItems
                .Select((item, index) => new WritingResearchPaa
                {
                    Question = item.Question,
                    SerpAnswerPreview = item.AnswerPreview,
                    Depth = 1,
                    DisplayOrder = index + 1,
                })
                .ToList(),
            RelatedSearches = pasfQueries
                .Select((text, index) => new WritingResearchPasf
                {
                    SearchText = text,
                    DisplayOrder = index + 1,
                })
                .ToList(),
            Competitors = competitors,
            SourceHeadings = sourceHeadings,
            BusinessContext = string.Empty,
            RecommendedTerms = recommendedTerms,
            ClosingFaqs = closingFaqs,
            SectionHints = sectionHints,
            CitationCandidates = export.CitationCandidates
                .Select(c => new WritingResearchCitationCandidate
                {
                    Url = c.Url,
                    Title = c.Title ?? string.Empty,
                    Domain = c.Domain ?? string.Empty,
                    Source = string.IsNullOrWhiteSpace(c.Source) ? "organic" : c.Source.Trim(),
                })
                .ToList(),
        };
    }

    private static IReadOnlyList<string> CollectPasfQueries(IReadOnlyList<ContentWriterSerpItem> serp) =>
        serp
            .Where(i => string.Equals(i.Type, "related_searches", StringComparison.OrdinalIgnoreCase))
            .SelectMany(i => i.RelatedQuestions ?? [])
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Select(q => q.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IReadOnlyList<(string Question, string AnswerPreview)> CollectPaaItems(
        IReadOnlyList<ContentWriterSerpItem> serp)
    {
        var items = new List<(string Question, string AnswerPreview)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in serp.Where(i =>
                     string.Equals(i.Type, "people_also_ask", StringComparison.OrdinalIgnoreCase)))
        {
            var answerPreview = item.Snippet ?? string.Empty;

            if (item.RelatedQuestions is { Count: > 0 })
            {
                foreach (var question in item.RelatedQuestions.Where(q => !string.IsNullOrWhiteSpace(q)))
                {
                    var trimmed = question.Trim();
                    if (seen.Add(trimmed))
                        items.Add((trimmed, answerPreview));
                }
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.Title) && seen.Add(item.Title.Trim()))
                items.Add((item.Title.Trim(), answerPreview));
        }

        return items;
    }

    private static WritingResearchOrganic MapOrganic(ContentWriterSerpItem item) => new()
    {
        Position = item.Position,
        Url = item.Url ?? string.Empty,
        Domain = item.Domain ?? string.Empty,
        Title = item.Title ?? string.Empty,
        Snippet = item.Snippet ?? string.Empty,
        ContentType = InferContentType(item),
    };

    private static string InferContentType(ContentWriterSerpItem item)
    {
        var haystack = $"{item.Url} {item.Title}".ToLowerInvariant();
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

    private static WritingResearchPaf BuildPaf(ContentWriterSerpItem? aiOverview)
    {
        if (aiOverview is null)
        {
            return new WritingResearchPaf
            {
                Type = "none",
                Format = "paragraph",
                BeatStrategy = "Open with a concise definition and outcome in the first 2–3 sentences.",
            };
        }

        return new WritingResearchPaf
        {
            Type = "ai_overview",
            Format = "mixed",
            Text = SerpCaptureTextSanitizer.Sanitize(aiOverview.Snippet) ?? string.Empty,
            BeatStrategy = string.Empty,
        };
    }

    private static string BuildDirectAnswerInstruction(string keyword, WritingResearchPaf paf)
    {
        if (string.Equals(paf.Type, "ai_overview", StringComparison.OrdinalIgnoreCase))
            return SerpFeatureGuidanceBuilder.BuildAiOverviewDraftInstruction(keyword);

        if (paf.Type is "none")
            return $"Open with a direct answer to \"{keyword}\" in 2–3 sentences: definition, who it helps, and the primary outcome.";

        return $"Open with a direct answer to \"{keyword}\" in 2–3 sentences: definition, who it helps, and the primary outcome. Match or beat the {paf.Type.Replace('_', ' ')} format ({paf.Format}).";
    }

    private static IReadOnlyList<WritingResearchCompetitor> MapCrawledCompetitors(
        IReadOnlyList<ContentWriterCompetitorExport> competitors) =>
        competitors
            .OrderBy(c => c.SeedRankAbsolute)
            .Select(c =>
            {
                var schemaTypes = c.SchemaTypes
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new WritingResearchCompetitor
                {
                    Url = c.Url,
                    Position = c.SeedRankAbsolute,
                    H1 = c.Headings.FirstOrDefault(h => h.Level == 1)?.Text
                        ?? c.Headings.FirstOrDefault()?.Text
                        ?? string.Empty,
                    EstimatedWordCount = c.WordCountEstimate > 0 ? c.WordCountEstimate : 1200,
                    Headings = c.Headings
                        .OrderBy(h => h.Sequence)
                        .Select((h, index) => new WritingResearchHeading
                        {
                            Level = h.Level,
                            Text = h.Text,
                            DisplayOrder = h.Sequence > 0 ? h.Sequence : index + 1,
                        })
                        .ToList(),
                    SchemaTypes = schemaTypes,
                    HasFaqSchema = c.HasFaqSchema
                        || schemaTypes.Any(t => string.Equals(t, "FAQPage", StringComparison.OrdinalIgnoreCase)),
                };
            })
            .ToList();

    private static WritingResearchBenchmarks BuildBenchmarks(
        IReadOnlyList<ContentWriterSerpItem> organic,
        ContentWriterExportBenchmarks exportBenchmarks)
    {
        if (exportBenchmarks.MedianWordCountTop5 > 0 || exportBenchmarks.MedianH2CountTop5 > 0)
        {
            var titleLengths = organic
                .Select(o => (o.Title ?? string.Empty).Length)
                .Where(len => len > 0)
                .OrderBy(len => len)
                .ToList();

            var medianTitle = titleLengths.Count == 0
                ? 55
                : titleLengths[titleLengths.Count / 2];

            var guideCount = organic.Count(o => InferContentType(o) == "guide");
            var dominantFormat = guideCount >= 3 ? "how_to" : "mixed";

            return new WritingResearchBenchmarks
            {
                MedianWordCountTop5 = exportBenchmarks.MedianWordCountTop5 > 0
                    ? exportBenchmarks.MedianWordCountTop5
                    : 1200,
                MedianTitleLengthTop10 = medianTitle,
                MedianH2CountTop5 = exportBenchmarks.MedianH2CountTop5 > 0
                    ? exportBenchmarks.MedianH2CountTop5
                    : 4,
                DominantContentFormat = dominantFormat,
            };
        }

        return BuildBenchmarksFromSerp(organic);
    }

    private static WritingResearchBenchmarks BuildBenchmarksFromSerp(IReadOnlyList<ContentWriterSerpItem> organic)
    {
        var titleLengths = organic
            .Select(o => (o.Title ?? string.Empty).Length)
            .Where(len => len > 0)
            .OrderBy(len => len)
            .ToList();

        var medianTitle = titleLengths.Count == 0
            ? 55
            : titleLengths.Count % 2 == 1
                ? titleLengths[titleLengths.Count / 2]
                : (titleLengths[titleLengths.Count / 2 - 1] + titleLengths[titleLengths.Count / 2]) / 2;

        var medianWords = organic.Count == 0
            ? 1200
            : organic.Take(5).Select(o => EstimateWordCount(o.Snippet)).OrderBy(v => v).ElementAt(organic.Take(5).Count() / 2);

        var guideCount = organic.Count(o => InferContentType(o) == "guide");
        var dominantFormat = guideCount >= 3 ? "how_to" : "mixed";

        return new WritingResearchBenchmarks
        {
            MedianWordCountTop5 = medianWords,
            MedianTitleLengthTop10 = medianTitle,
            MedianH2CountTop5 = 4,
            DominantContentFormat = dominantFormat,
        };
    }

    private static (string Primary, string Justification) InferIntent(
        string keyword,
        IReadOnlyList<WritingResearchOrganic> organic)
    {
        var lower = keyword.ToLowerInvariant();
        var guideCount = organic.Count(o => o.ContentType is "guide" or "other");

        if (lower.Contains("buy", StringComparison.Ordinal) || lower.Contains("price", StringComparison.Ordinal)
            || lower.Contains("cost", StringComparison.Ordinal))
        {
            return ("commercial", "Query language skews toward pricing or purchase evaluation.");
        }

        if (guideCount >= 3 || lower.Contains("how ", StringComparison.Ordinal) || lower.Contains("what is", StringComparison.Ordinal))
        {
            return ("informational", "SERP is dominated by guides and definitional content.");
        }

        return ("informational", "Default informational classification from organic title/snippet mix.");
    }

    private static IReadOnlyList<WritingResearchTerm> BuildRecommendedTerms(
        string keyword,
        IReadOnlyList<string> pasfQueries,
        IReadOnlyList<string> paaQuestions,
        IReadOnlyList<ContentWriterSerpItem> organic)
    {
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

        foreach (var pasf in pasfQueries)
            Add(pasf);

        foreach (var paa in paaQuestions)
            Add(paa);

        foreach (var token in keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            Add(token);

        foreach (var title in organic.Take(5).Select(o => o.Title))
            Add(title);

        return terms
            .Take(20)
            .Select((term, index) => new WritingResearchTerm { Term = term, DisplayOrder = index + 1 })
            .ToList();
    }

    private static IReadOnlyList<WritingResearchClosingFaq> BuildClosingFaqs(
        string keyword,
        IReadOnlyList<(string Question, string AnswerPreview)> paaItems,
        IReadOnlyList<string> pasfQueries)
    {
        var items = new List<WritingResearchClosingFaq>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string question, string source)
        {
            var trimmed = question.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || !seen.Add(trimmed))
                return;
            items.Add(new WritingResearchClosingFaq
            {
                Question = trimmed,
                Source = source,
                DisplayOrder = items.Count + 1,
            });
        }

        foreach (var (question, _) in paaItems)
        {
            Add(question, "paa");
            if (items.Count >= ContentWritingRules.ClosingFaqCount)
                return items;
        }

        foreach (var related in pasfQueries)
        {
            var question = related.Contains('?', StringComparison.Ordinal)
                ? related
                : $"What should I know about {related}?";
            Add(question, "pasf");
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

    private static IReadOnlyList<WritingResearchSectionHint> BuildSectionHints(
        string keyword,
        IReadOnlyList<ContentWriterSerpItem> organic,
        IReadOnlyList<string> paaQuestions,
        IReadOnlyList<string> pasfQueries)
    {
        var phases = WritingMethodologySpec.FourPhase.PhaseDefinitions;
        var titlePool = organic
            .Select(o => o.Title)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => title!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var subtopicPool = paaQuestions
            .Concat(pasfQueries)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hints = new List<WritingResearchSectionHint>();
        for (var i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            var suggestedH2 = titlePool.ElementAtOrDefault(i);
            if (string.IsNullOrWhiteSpace(suggestedH2))
                suggestedH2 = ArticleMethodologyScaffold.SuggestTopicHeading(keyword, phase);

            var subtopics = subtopicPool
                .Skip(i * 2)
                .Take(3)
                .ToList();

            hints.Add(new WritingResearchSectionHint
            {
                DisplayOrder = i + 1,
                Movement = i + 1,
                Label = phase.Label,
                SuggestedH2 = suggestedH2,
                SubtopicsFromSerp = subtopics,
            });
        }

        return hints;
    }

    private static int EstimateWordCount(string? snippet) =>
        string.IsNullOrWhiteSpace(snippet) ? 1200 : Math.Max(800, snippet.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length * 12);
}
