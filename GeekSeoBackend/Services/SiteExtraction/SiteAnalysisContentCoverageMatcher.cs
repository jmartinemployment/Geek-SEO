using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Maps crawled URLs and fusion signals to pillar/subtopic coverage status (step 12).
/// </summary>
internal static class SiteContentCoverageMatcher
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
        IReadOnlyList<SiteAnalysisPillar> pillars,
        IReadOnlyList<SiteAnalysisSubtopic> subtopics,
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
        IReadOnlyList<SiteAnalysisSubtopic> pillarSubtopics,
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
        SiteAnalysisSubtopic subtopic,
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

    private static bool SubtopicMatchesChildSlug(SiteAnalysisSubtopic subtopic, string childSlug)
    {
        var titleSlug = Slugify(subtopic.SubtopicTitle);
        return titleSlug.Contains(childSlug, StringComparison.OrdinalIgnoreCase)
            || childSlug.Contains(titleSlug, StringComparison.OrdinalIgnoreCase);
    }

    private static void PopulateExistingPages(
        SiteAnalysisPillar pillar,
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

            pillar.ExistingPages.Add(new SiteAnalysisPillarPage
            {
                Id = Guid.NewGuid(),
                PillarId = pillar.Id,
                Url = page.Url,
                RelevanceScore = relevance,
                CoverageQuality = SiteAuthorityScorer.DetermineCoverageQuality(relevance),
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

    /// <summary>
    /// True when <paramref name="url"/>'s path already represents <paramref name="slug"/>.
    /// Slugs are lowercase with hyphens between words (see <see cref="SiteAnalyzerService.NameToSlug"/>).
    /// A page is not missing when its slug is found as a path segment, or (for longer slugs) in the path.
    /// </summary>
    private static bool UrlPathContainsSlug(string url, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        try
        {
            var path = new Uri(url).AbsolutePath;
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // Exact path segment (covers short slugs like "ai" without matching inside "training").
            if (segments.Any(s => string.Equals(s, slug, StringComparison.OrdinalIgnoreCase)))
                return true;
            // Longer slugs: also allow hyphen/path forms in the full path.
            if (slug.Length < 3)
                return false;
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
        SiteAnalyzerService.NameToSlug(value);

    /// <summary>
    /// Missing-page check: heading (any length slug) whose slug is not found on any crawled/sitemap URL.
    /// Slugs are lowercase with hyphens between words.
    /// </summary>
    internal static bool HasNoMatchingPage(string headingText, IReadOnlyList<string> allUrls)
    {
        var slug = Slugify(headingText);
        if (string.IsNullOrWhiteSpace(slug))
            return false;
        return !allUrls.Any(u => UrlPathContainsSlug(u, slug));
    }

    /// <summary>Whether a page URL's path relates to the given pillar slug — used to scope real
    /// per-page headings to the pillar they were found under for gap detection.</summary>
    internal static bool UrlBelongsToPillarSlug(string url, string pillarSlug) =>
        UrlPathContainsSlug(url, pillarSlug);

    /// <summary>
    /// Walks per-page heading trees (h1–h6) and returns headings under <paramref name="pillarSlug"/>
    /// that have no dedicated page — missing-page candidates. Bare headings count; short slugs (e.g. "ai") count.
    /// </summary>
    internal static IReadOnlyList<(string HeadingText, string HeadingSlug)> CollectTreeGaps(
        string pillarSlug,
        IReadOnlyList<(string PageUrl, IReadOnlyList<PageSection> Tree)> pageTrees,
        IReadOnlyList<string> allUrls)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { pillarSlug };
        var gaps = new List<(string HeadingText, string HeadingSlug)>();

        foreach (var (pageUrl, tree) in pageTrees)
        {
            var pageBelongs = UrlBelongsToPillarSlug(pageUrl, pillarSlug);
            foreach (var node in EnumerateGapCandidates(tree, pillarSlug, pageBelongs))
            {
                if (!HasNoMatchingPage(node.HeadingText, allUrls))
                    continue;

                var headingSlug = Slugify(node.HeadingText);
                if (string.IsNullOrWhiteSpace(headingSlug) || !seen.Add(headingSlug))
                    continue;

                gaps.Add((node.HeadingText, headingSlug));
            }
        }

        return gaps;
    }

    /// <summary>
    /// Yields heading nodes in scope for a pillar: every node on a pillar-owned page, plus nodes
    /// nested under a heading whose slug matches the pillar (e.g. homepage h5s under an h4 pillar).
    /// </summary>
    internal static IEnumerable<PageSection> EnumerateGapCandidates(
        IReadOnlyList<PageSection> nodes,
        string pillarSlug,
        bool pageBelongsToPillar,
        bool underPillarHeading = false)
    {
        foreach (var node in nodes)
        {
            var nodeSlug = Slugify(node.HeadingText);
            var isPillarHeading = string.Equals(nodeSlug, pillarSlug, StringComparison.OrdinalIgnoreCase);
            var inScope = pageBelongsToPillar || underPillarHeading || isPillarHeading;

            if (inScope && !isPillarHeading)
                yield return node;

            foreach (var child in EnumerateGapCandidates(
                         node.Children, pillarSlug, pageBelongsToPillar, underPillarHeading || isPillarHeading))
                yield return child;
        }
    }

    /// <summary>
    /// Content Creator's entire gap rule: every real heading (h1–h6, any page, any depth) with no
    /// matching crawled/sitemap page. No pillar scoping, no confidence score, no minimum length —
    /// a heading as short as "AI" with no <c>/ai</c> page counts. This is deliberately not scoped
    /// to a pillar/topic-selection concept at all; see <see cref="CollectTreeGaps"/> for the
    /// pillar-scoped variant this app's own coverage step used.
    /// </summary>
    internal static IReadOnlyList<(string HeadingText, string? ParentHeadingText)> CollectAllHeadingGaps(
        IReadOnlyList<(string PageUrl, IReadOnlyList<PageSection> Tree)> pageTrees,
        IReadOnlyList<string> allUrls)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gaps = new List<(string HeadingText, string? ParentHeadingText)>();

        var pageHeadings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, tree) in pageTrees)
        {
            var firstHeading = tree.FirstOrDefault()?.HeadingText;
            if (!string.IsNullOrWhiteSpace(firstHeading))
                pageHeadings.Add(firstHeading.ToLowerInvariant());
        }

        foreach (var (_, tree) in pageTrees)
        {
            foreach (var (node, parent) in EnumerateAllHeadings(tree, null))
            {
                if (!HasNoMatchingPage(node.HeadingText, allUrls))
                    continue;

                if (pageHeadings.Contains(node.HeadingText.ToLowerInvariant()))
                    continue;

                var slug = Slugify(node.HeadingText);
                if (string.IsNullOrWhiteSpace(slug) || !seen.Add(slug))
                    continue;

                gaps.Add((node.HeadingText, parent?.HeadingText));
            }
        }

        return gaps;
    }

    private static IEnumerable<(PageSection Node, PageSection? Parent)> EnumerateAllHeadings(
        IReadOnlyList<PageSection> nodes,
        PageSection? parent)
    {
        foreach (var node in nodes)
        {
            yield return (node, parent);
            foreach (var child in EnumerateAllHeadings(node.Children, node))
                yield return child;
        }
    }
}
