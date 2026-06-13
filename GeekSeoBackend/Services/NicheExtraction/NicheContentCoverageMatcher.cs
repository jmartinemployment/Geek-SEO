using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Maps crawled URLs and fusion signals to pillar/subtopic coverage status (step 12).
/// </summary>
internal static class NicheContentCoverageMatcher
{
    private const decimal TopicalityCoveredThreshold = 0.06m;
    private const decimal TopicalityPartialThreshold = 0.015m;

    internal sealed record ContentCoverageResult(
        int PillarsCovered,
        int PillarsPartial,
        int PillarsGap,
        int SubtopicsCovered,
        int SubtopicsTotal,
        IReadOnlyList<string> SamplePartialPillars);

    internal static ContentCoverageResult Apply(
        IReadOnlyList<NichePillar> pillars,
        IReadOnlyList<NicheSubtopic> subtopics,
        SiteTopicProfile fused,
        IReadOnlyList<DiscoveredPillar> discovered,
        SiteCrawlData crawl,
        SitemapData sitemap,
        IReadOnlyList<PillarSerpEnrichment> serpValidations)
    {
        if (pillars.Count == 0)
        {
            return new ContentCoverageResult(0, 0, 0, 0, 0, []);
        }

        var entityCoverage = EntityCoverageScorer.Compute(fused, serpValidations);
        var candidatesBySlug = fused.SelectedPillars.ToDictionary(
            p => p.Slug,
            StringComparer.OrdinalIgnoreCase);
        var discoveredBySlug = discovered.ToDictionary(
            d => d.Slug,
            StringComparer.OrdinalIgnoreCase);

        var allUrls = crawl.Pages
            .Select(p => p.Url)
            .Concat(sitemap.SampleUrls)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var coveredPillars = 0;
        var partialPillars = 0;
        var gapPillars = 0;
        var partialSamples = new List<string>();
        var subtopicsCovered = 0;

        foreach (var pillar in pillars)
        {
            candidatesBySlug.TryGetValue(pillar.PillarSlug, out var candidate);
            discoveredBySlug.TryGetValue(pillar.PillarSlug, out var disc);

            var dedicatedUrl = FirstNonEmpty(
                pillar.PageUrl,
                candidate?.DedicatedPageUrl,
                disc?.PageUrl);

            fused.NormalizedTopicalityBySlug.TryGetValue(pillar.PillarSlug, out var topicality);
            entityCoverage.TryGetValue(pillar.PillarSlug, out var entityCov);
            var isEntityThin = entityCov?.IsEntityThin ?? false;

            var pillarSubtopics = subtopics
                .Where(s => s.PillarId == pillar.Id)
                .ToList();

            MatchSubtopics(pillarSubtopics, allUrls, disc?.ChildSlugs ?? []);

            var coveredSubs = pillarSubtopics.Count(s =>
                string.Equals(s.CoverageStatus, "covered", StringComparison.OrdinalIgnoreCase));
            subtopicsCovered += coveredSubs;

            pillar.CoveredSubtopicCount = coveredSubs;
            pillar.ExistingPageCount = CountMatchingPages(pillar.PillarSlug, dedicatedUrl, allUrls);
            PopulateExistingPages(pillar, crawl, dedicatedUrl, topicality, pillar.PillarSlug);

            pillar.CoverageStatus = ClassifyPillar(
                dedicatedUrl,
                topicality,
                coveredSubs,
                pillar.RequiredSubtopicCount,
                candidate?.InternalLinkCount ?? 0,
                isEntityThin,
                pillar.ExistingPageCount);

            switch (pillar.CoverageStatus)
            {
                case "covered":
                    coveredPillars++;
                    break;
                case "partial":
                    partialPillars++;
                    if (partialSamples.Count < 8)
                        partialSamples.Add(pillar.PillarTopic);
                    break;
                default:
                    gapPillars++;
                    break;
            }
        }

        return new ContentCoverageResult(
            coveredPillars,
            partialPillars,
            gapPillars,
            subtopicsCovered,
            subtopics.Count,
            partialSamples);
    }

    private static string ClassifyPillar(
        string? dedicatedUrl,
        decimal topicality,
        int coveredSubtopics,
        int requiredSubtopics,
        int internalLinkCount,
        bool isEntityThin,
        int existingPageCount = 0)
    {
        // A dedicated URL must have been successfully crawled (existingPageCount > 0)
        // — a URL string from nav/schema that returns 404 is not a real page.
        var hasDedicatedPage = !string.IsNullOrWhiteSpace(dedicatedUrl)
            && !TopicClusteringService.IsHomepageUrl(dedicatedUrl)
            && existingPageCount > 0;

        var subtopicRatio = requiredSubtopics > 0
            ? (decimal)coveredSubtopics / requiredSubtopics
            : 0m;

        if (hasDedicatedPage
            && !isEntityThin
            && (subtopicRatio >= 0.4m || topicality >= TopicalityCoveredThreshold || internalLinkCount >= 2))
        {
            return "covered";
        }

        if (hasDedicatedPage
            || coveredSubtopics > 0
            || topicality >= TopicalityPartialThreshold
            || internalLinkCount > 0)
        {
            return "partial";
        }

        return "gap";
    }

    private static void MatchSubtopics(
        IReadOnlyList<NicheSubtopic> pillarSubtopics,
        IReadOnlyList<string> allUrls,
        IReadOnlyList<string> childSlugs)
    {
        foreach (var subtopic in pillarSubtopics)
        {
            var matchedUrl = FindSubtopicUrl(subtopic, allUrls, childSlugs);
            if (matchedUrl is null)
                continue;

            subtopic.CoverageStatus = "covered";
            subtopic.ExistingUrl = matchedUrl;
            subtopic.FixEffort = "optimize";
        }
    }

    private static string? FindSubtopicUrl(
        NicheSubtopic subtopic,
        IReadOnlyList<string> allUrls,
        IReadOnlyList<string> childSlugs)
    {
        foreach (var childSlug in childSlugs)
        {
            if (!SubtopicMatchesChildSlug(subtopic, childSlug))
                continue;

            var url = allUrls.FirstOrDefault(u =>
                UrlPathContainsSlug(u, childSlug));
            if (url is not null)
                return url;
        }

        var keywordSlug = Slugify(subtopic.TargetKeyword);
        if (keywordSlug.Length >= 4)
        {
            return allUrls.FirstOrDefault(u => UrlPathContainsSlug(u, keywordSlug));
        }

        return null;
    }

    private static bool SubtopicMatchesChildSlug(NicheSubtopic subtopic, string childSlug)
    {
        var titleSlug = Slugify(subtopic.SubtopicTitle);
        return titleSlug.Contains(childSlug, StringComparison.OrdinalIgnoreCase)
            || childSlug.Contains(titleSlug, StringComparison.OrdinalIgnoreCase);
    }

    private static void PopulateExistingPages(
        NichePillar pillar,
        SiteCrawlData crawl,
        string? dedicatedUrl,
        decimal topicality,
        string pillarSlug)
    {
        pillar.ExistingPages.Clear();

        foreach (var page in crawl.Pages)
        {
            if (!UrlRelatesToPillar(page.Url, pillarSlug, dedicatedUrl))
                continue;

            var relevance = topicality >= TopicalityCoveredThreshold
                ? 85m
                : topicality >= TopicalityPartialThreshold
                    ? 55m
                    : 35m;

            pillar.ExistingPages.Add(new NichePillarPage
            {
                Id = Guid.NewGuid(),
                PillarId = pillar.Id,
                Url = page.Url,
                RelevanceScore = relevance,
                CoverageQuality = NicheAuthorityScorer.DetermineCoverageQuality(relevance),
                WordCount = NormalizedTopicalityCalculator.EstimateWordCount(page.Html),
            });
        }
    }

    private static bool UrlRelatesToPillar(string url, string pillarSlug, string? dedicatedUrl)
    {
        if (!string.IsNullOrWhiteSpace(dedicatedUrl)
            && UrlsMatch(url, dedicatedUrl))
        {
            return true;
        }

        return UrlPathContainsSlug(url, pillarSlug);
    }

    private static int CountMatchingPages(string pillarSlug, string? dedicatedUrl, IReadOnlyList<string> allUrls) =>
        allUrls.Count(u => UrlRelatesToPillar(u, pillarSlug, dedicatedUrl));

    private static bool UrlPathContainsSlug(string url, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length < 3)
            return false;

        try
        {
            var path = new Uri(url).AbsolutePath;
            return path.Contains(slug, StringComparison.OrdinalIgnoreCase)
                || path.Contains(slug.Replace('-', '/'), StringComparison.OrdinalIgnoreCase);
        }
        catch (UriFormatException)
        {
            return url.Contains(slug, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool UrlsMatch(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var left = new Uri(a);
            var right = new Uri(b);
            return string.Equals(left.AbsolutePath.TrimEnd('/'), right.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string Slugify(string value) =>
        NicheAnalyzerService.NameToSlug(value);
}
