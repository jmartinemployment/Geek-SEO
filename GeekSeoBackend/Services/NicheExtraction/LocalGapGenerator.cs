using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Step 11 — compares schema <c>areaServed</c> to location URLs discovered via sitemap, crawl, and URL patterns.
/// </summary>
internal static class LocalGapGenerator
{
    private static readonly string[] LocalPathSegments =
    [
        "locations", "location", "areas", "area", "service-areas", "service-area",
        "cities", "city", "regions", "region", "markets", "market",
    ];

    private static readonly HashSet<string> GeoStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "county", "counties", "city", "cities", "state", "region", "regions", "area", "areas",
        "fl", "florida", "usa", "us", "united", "states", "the", "and", "of", "in", "for",
    };

    internal static LocalGeographyAnalysis Analyze(
        SchemaOrgData schema,
        SitemapData sitemap,
        IReadOnlyList<string> crawlUrls,
        UrlPatternData urlPatterns,
        IReadOnlyList<DiscoveredPillar> mergedPillars)
    {
        var areasServed = schema.AreaServed
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var locationPages = CollectLocationPages(sitemap, crawlUrls, urlPatterns, mergedPillars);
        var isLocalBusiness = areasServed.Count > 0
            || mergedPillars.Any(p => string.Equals(p.Intent, "local", StringComparison.OrdinalIgnoreCase))
            || locationPages.Count > 0;

        if (!isLocalBusiness)
        {
            return new LocalGeographyAnalysis(
                [],
                [],
                [],
                IsLocalBusiness: false);
        }

        var gaps = new List<LocalGeographyGap>();
        foreach (var area in areasServed)
        {
            if (locationPages.Any(page => AreaMatchesLocation(area, page.Slug, page.Name)))
                continue;

            var slug = NicheAnalyzerService.NameToSlug(area);
            gaps.Add(new LocalGeographyGap(
                area,
                slug,
                BuildSuggestedTitle(area),
                $"Schema declares \"{area}\" in areaServed but no matching /locations or /areas page was found."));
        }

        return new LocalGeographyAnalysis(
            areasServed,
            locationPages,
            gaps,
            IsLocalBusiness: true);
    }

    private static List<LocalLocationPage> CollectLocationPages(
        SitemapData sitemap,
        IReadOnlyList<string> crawlUrls,
        UrlPatternData urlPatterns,
        IReadOnlyList<DiscoveredPillar> mergedPillars)
    {
        var bySlug = new Dictionary<string, LocalLocationPage>(StringComparer.OrdinalIgnoreCase);

        void Add(string name, string slug, string url, string source)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return;
            if (bySlug.ContainsKey(slug))
                return;

            bySlug[slug] = new LocalLocationPage(name, slug, url, source);
        }

        foreach (var pillar in sitemap.Pillars.Where(p =>
                     string.Equals(p.Intent, "local", StringComparison.OrdinalIgnoreCase)))
        {
            Add(pillar.Name, pillar.Slug, pillar.PageUrl ?? string.Empty, "sitemap");
        }

        foreach (var pillar in mergedPillars.Where(p =>
                     string.Equals(p.Intent, "local", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(p.PageUrl)))
        {
            Add(pillar.Name, pillar.Slug, pillar.PageUrl!, "pillar");
        }

        foreach (var topic in urlPatterns.Topics)
        {
            if (!IsLocalPathSegment(topic.PathSegment) && !IsLocalUrl(topic.Url))
                continue;

            Add(topic.Name, topic.Slug, topic.Url, "url_pattern");
        }

        var allUrls = sitemap.SampleUrls
            .Concat(crawlUrls)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var url in allUrls)
        {
            if (!TryParseLocalUrl(url, out var name, out var slug))
                continue;

            Add(name, slug, url, "crawl");
        }

        return bySlug.Values
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static bool AreaMatchesLocation(string areaName, string slug, string pageName)
    {
        var significant = Tokenize(areaName)
            .Where(t => t.Length >= 3 && !GeoStopWords.Contains(t))
            .ToList();

        if (significant.Count == 0)
            return false;

        var targetTokens = Tokenize(slug)
            .Concat(Tokenize(pageName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return significant.Any(token =>
            targetTokens.Any(t =>
                t.Contains(token, StringComparison.OrdinalIgnoreCase)
                || token.Contains(t, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool TryParseLocalUrl(string url, out string name, out string slug)
    {
        name = string.Empty;
        slug = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split('?', '#')[0])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (segments.Count < 2)
            return false;

        if (!IsLocalPathSegment(segments[0]))
            return false;

        slug = segments[1];
        if (NoisePaths.IsNoise(slug))
            return false;

        name = SitemapExtractor.SlugToTitle(slug);
        return true;
    }

    private static bool IsLocalUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split('?', '#')[0])
            .ToList();

        return segments.Count >= 2 && IsLocalPathSegment(segments[0]);
    }

    private static bool IsLocalPathSegment(string segment) =>
        LocalPathSegments.Contains(segment, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> Tokenize(string value) =>
        value.Split([' ', '-', '_', ',', '.', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0);

    private static string BuildSuggestedTitle(string areaName) =>
        $"Services in {areaName}";
}
