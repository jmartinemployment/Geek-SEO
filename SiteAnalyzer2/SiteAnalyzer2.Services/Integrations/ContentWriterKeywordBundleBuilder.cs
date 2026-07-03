using GeekSeo.Application.Services.Seo;
using SiteAnalyzer2.Domain;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Domain.Enums;
using SiteAnalyzer2.Serp;

namespace SiteAnalyzer2.Services.Integrations;

public static class ContentWriterKeywordBundleBuilder
{
    public static ContentWriterSerpExportDto Build(
        AnalysisRun run,
        IReadOnlyList<SerpItem> serpItems,
        IReadOnlyList<CompetitorPage> competitorPages,
        IReadOnlyList<Page> sourcePages,
        DateTimeOffset capturedAt,
        IReadOnlyList<string>? authorityPageUrls = null)
    {
        var keywordItems = serpItems
            .Where(i => string.IsNullOrWhiteSpace(i.ResearchLane))
            .ToList();
        var supplementalItems = serpItems
            .Where(i => !string.IsNullOrWhiteSpace(i.ResearchLane))
            .ToList();

        var serp = BuildSerpItems(keywordItems);
        var manualLanes = BuildManualResearchLanes(supplementalItems, run.Keyword);
        var competitors = BuildCompetitors(competitorPages);
        var sourceHeadings = BuildSourceHeadings(run, sourcePages);
        var benchmarks = BuildBenchmarks(competitorPages, competitors);
        var citationCandidates = BuildCitationCandidates(keywordItems, authorityPageUrls ?? []);

        return new ContentWriterSerpExportDto
        {
            BundleVersion = ContentWriterSerpExportDto.CurrentBundleVersion,
            CapturedAt = capturedAt,
            RunId = run.Id,
            ProjectId = run.ProjectId,
            Keyword = SerpSearchKeywordNormalizer.Normalize(run.Keyword),
            TargetSiteUrl = run.TargetSiteUrl,
            Status = run.Status.ToString(),
            SerpSeResultsCount = run.SerpSeResultsCount ?? 0,
            SerpCapturedAt = ToOffset(run.SerpCapturedAt),
            CompetitorCrawlStatus = run.CompetitorCrawlStatus,
            CompetitorCrawlFinishedAt = ToOffset(run.CompetitorCrawlFinishedAt),
            MatchedPillarTopic = run.MatchedPillarTopic,
            MatchedPillarIntent = run.MatchedPillarIntent,
            MatchedPillarAngle = run.MatchedPillarAngle,
            GapTopics = run.GapTopics.ToList(),
            WritingInstructions = run.WritingInstructions,
            WritingRecommendations = BuildKeywordWritingRecommendations(run),
            Serp = serp,
            SourceHeadings = sourceHeadings,
            Competitors = competitors,
            Benchmarks = benchmarks,
            CitationCandidates = citationCandidates,
            ResearchMode = run.ResearchMode,
            TopicSlug = run.TopicSlug,
            ManualResearchLanes = manualLanes,
        };
    }

    private static List<ContentWriterManualResearchLaneDto> BuildManualResearchLanes(
        IReadOnlyList<SerpItem> supplementalItems,
        string keyword)
    {
        return supplementalItems
            .GroupBy(i => i.ResearchLane!, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var laneItems = group.ToList();
                var organics = laneItems
                    .Where(i => string.Equals(i.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase) && !i.Ads)
                    .ToList();
                var paaQuestions = string.Equals(group.Key, SerpResearchLanes.Paa, StringComparison.OrdinalIgnoreCase)
                    ? PaaQuestionRelevanceFilter.Filter(keyword, CollectPaaQuestions(laneItems)).ToList()
                    : CollectPaaQuestions(laneItems);
                return new ContentWriterManualResearchLaneDto
                {
                    Lane = group.Key,
                    Label = SerpResearchLanes.DisplayLabel(group.Key),
                    OrganicCount = organics.Count,
                    PaaCount = paaQuestions.Count,
                    OrganicResults = organics.Select((item, index) => new ContentWriterSerpItemDto
                    {
                        Position = item.RankGroup > 0 ? item.RankGroup : index + 1,
                        Type = SerpItemTypes.Organic,
                        Title = item.Title,
                        Url = item.Url,
                        Domain = item.Domain,
                        Snippet = BuildSnippet(item),
                        Date = item.PreSnippet,
                        SiteName = item.WebsiteName,
                    }).ToList(),
                    PaaQuestions = paaQuestions,
                };
            })
            .OrderBy(l => l.Lane, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> CollectPaaQuestions(IReadOnlyList<SerpItem> items)
    {
        var questions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            foreach (var query in item.RelatedQueries.OrderBy(q => q.Sequence))
            {
                if (string.IsNullOrWhiteSpace(query.QueryText))
                    continue;

                var trimmed = query.QueryText.Trim();
                if (seen.Add(trimmed))
                    questions.Add(trimmed);
            }
        }

        return questions;
    }

    internal static List<ContentWriterCitationCandidateDto> BuildCitationCandidates(
        IReadOnlyList<SerpItem> serpItems,
        IReadOnlyList<string> authorityPageUrls)
    {
        _ = serpItems;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<ContentWriterCitationCandidateDto>();

        foreach (var url in authorityPageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Take(8))
        {
            var trimmed = url.Trim();
            if (!seen.Add(trimmed))
                continue;

            candidates.Add(new ContentWriterCitationCandidateDto
            {
                Url = trimmed,
                Source = "authority",
            });
        }

        return candidates;
    }

    private static List<ContentWriterSerpItemDto> BuildSerpItems(IReadOnlyList<SerpItem> items)
    {
        var serp = new List<ContentWriterSerpItemDto>();
        var position = 1;

        foreach (var item in items)
        {
            if (string.Equals(item.Type, SerpItemTypes.Organic, StringComparison.OrdinalIgnoreCase) && !item.Ads)
            {
                if (!SerpOrganicUrlQuality.IsUsableOrganicUrl(item.Url))
                    continue;

                serp.Add(new ContentWriterSerpItemDto
                {
                    Position = item.RankGroup > 0 ? item.RankGroup : position++,
                    Type = SerpItemTypes.Organic,
                    Title = item.Title,
                    Url = item.Url,
                    Domain = item.Domain,
                    Snippet = BuildSnippet(item),
                    Date = item.PreSnippet,
                    SiteName = item.WebsiteName,
                });
                continue;
            }

            if (string.Equals(item.Type, SerpItemTypes.AiOverview, StringComparison.OrdinalIgnoreCase))
            {
                serp.Add(new ContentWriterSerpItemDto
                {
                    Position = position++,
                    Type = SerpItemTypes.AiOverview,
                    Snippet = item.AiOverviewMarkdown
                        ?? item.AiOverviewStatusMessage
                        ?? item.Description,
                });
                continue;
            }

            if (string.Equals(item.Type, SerpItemTypes.RelatedSearches, StringComparison.OrdinalIgnoreCase))
            {
                var related = item.RelatedQueries
                    .OrderBy(q => q.Sequence)
                    .Select(q => q.QueryText)
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .ToList();
                if (related.Count > 0)
                {
                    serp.Add(new ContentWriterSerpItemDto
                    {
                        Position = position++,
                        Type = SerpItemTypes.RelatedSearches,
                        RelatedQuestions = related,
                    });
                }

                continue;
            }

            if (item.RelatedQueries.Count > 0
                && (string.Equals(item.Type, "people_also_ask", StringComparison.OrdinalIgnoreCase)
                    || item.RelatedQueries.Any(q =>
                        string.Equals(q.QueryType.ToString(), "PeopleAlsoAsk", StringComparison.OrdinalIgnoreCase))))
            {
                serp.Add(new ContentWriterSerpItemDto
                {
                    Position = position++,
                    Type = "people_also_ask",
                    RelatedQuestions = item.RelatedQueries
                        .OrderBy(q => q.Sequence)
                        .Select(q => q.QueryText)
                        .Where(q => !string.IsNullOrWhiteSpace(q))
                        .ToList(),
                    Snippet = item.Description,
                });
            }
        }

        return serp;
    }

    private static List<ContentWriterCompetitorExportDto> BuildCompetitors(IReadOnlyList<CompetitorPage> competitorPages)
    {
        if (competitorPages.Count == 0)
            return [];

        var pagesPerDomain = competitorPages
            .GroupBy(p => p.Domain, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return competitorPages
            .Where(p => p.DepthFromSeed == 0)
            .GroupBy(p => p.Domain, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(p => p.SeedRankAbsolute).First())
            .OrderBy(p => p.SeedRankAbsolute)
            .Select(page =>
            {
                var schemaTypes = ExtractSchemaTypes(page.JsonLdBlocks);
                return new ContentWriterCompetitorExportDto
                {
                    Domain = page.Domain,
                    Url = page.Url,
                    SeedRankAbsolute = page.SeedRankAbsolute,
                    PagesCrawledOnDomain = pagesPerDomain.GetValueOrDefault(page.Domain, 1),
                    Headings = MapHeadings(page.Headings),
                    WordCountEstimate = EstimateWordCountFromHeadings(page.Headings),
                    SchemaTypes = schemaTypes,
                    HasFaqSchema = schemaTypes.Any(t =>
                        string.Equals(t, "FAQPage", StringComparison.OrdinalIgnoreCase)),
                };
            })
            .ToList();
    }

    private static List<ContentWriterHeadingDto> BuildSourceHeadings(AnalysisRun run, IReadOnlyList<Page> sourcePages)
    {
        if (sourcePages.Count == 0)
            return [];

        var targetPage = sourcePages.FirstOrDefault(p => UrlsMatch(p.Url, run.TargetSiteUrl))
            ?? sourcePages.OrderBy(p => p.DepthFromHomepage ?? int.MaxValue).FirstOrDefault();

        return targetPage is null ? [] : MapHeadings(targetPage.Headings);
    }

    private static ContentWriterBenchmarksDto BuildBenchmarks(
        IReadOnlyList<CompetitorPage> competitorPages,
        IReadOnlyList<ContentWriterCompetitorExportDto> seedCompetitors)
    {
        var topFive = seedCompetitors.Take(5).ToList();
        var h2Counts = topFive
            .Select(c => c.Headings.Count(h => h.Level == 2))
            .ToList();
        var wordCounts = topFive
            .Select(c => c.WordCountEstimate)
            .Where(c => c > 0)
            .ToList();

        return new ContentWriterBenchmarksDto
        {
            MedianH2CountTop5 = Median(h2Counts),
            MedianWordCountTop5 = Median(wordCounts),
            CompetitorDomainCount = competitorPages
                .Select(p => p.Domain)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            CompetitorPageCount = competitorPages.Count,
        };
    }

    private static List<string> BuildKeywordWritingRecommendations(AnalysisRun run)
    {
        var recommendations = new List<string>();

        if (!string.IsNullOrWhiteSpace(run.MatchedPillarTopic))
        {
            var line = $"Keyword \"{run.Keyword.Trim()}\": align with pillar \"{run.MatchedPillarTopic.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(run.MatchedPillarIntent))
                line += $" ({run.MatchedPillarIntent.Trim()} intent)";
            if (!string.IsNullOrWhiteSpace(run.MatchedPillarAngle))
                line += $". Angle: {run.MatchedPillarAngle.Trim()}";
            recommendations.Add(line + ".");
        }

        if (run.GapTopics.Count > 0)
        {
            recommendations.Add(
                $"Content gaps for \"{run.Keyword.Trim()}\": {string.Join(", ", run.GapTopics.Take(5))}.");
        }

        return recommendations;
    }

    private static List<ContentWriterHeadingDto> MapHeadings(IEnumerable<PageHeading> headings) =>
        headings
            .OrderBy(h => h.Sequence)
            .Select(h => new ContentWriterHeadingDto
            {
                Level = h.Level,
                Text = h.Text,
                Sequence = h.Sequence,
            })
            .ToList();

    private static List<ContentWriterHeadingDto> MapHeadings(IEnumerable<CompetitorPageHeading> headings) =>
        headings
            .OrderBy(h => h.Sequence)
            .Select(h => new ContentWriterHeadingDto
            {
                Level = h.Level,
                Text = h.Text,
                Sequence = h.Sequence,
            })
            .ToList();

    private static List<string> ExtractSchemaTypes(IEnumerable<CompetitorPageJsonLd> blocks) =>
        blocks
            .Select(b => b.ParsedType)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static int EstimateWordCountFromHeadings(IEnumerable<CompetitorPageHeading> headings)
    {
        var text = string.Join(
            ' ',
            headings
                .OrderBy(h => h.Sequence)
                .Select(h => h.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static int Median(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
            return 0;

        var sorted = values.OrderBy(v => v).ToList();
        return sorted[sorted.Count / 2];
    }

    private static bool UrlsMatch(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        return string.Equals(NormalizeUrl(left), NormalizeUrl(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return url.Trim().TrimEnd('/');

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    private static string? BuildSnippet(SerpItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Description))
            return item.Description;
        if (!string.IsNullOrWhiteSpace(item.ExtendedSnippet))
            return item.ExtendedSnippet;
        return item.PreSnippet;
    }
}
