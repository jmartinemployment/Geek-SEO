using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Derives topic candidates from URL path segments (e.g. /services/accounting-software).
/// </summary>
public sealed class UrlPatternExtractor
{
    private static readonly HashSet<string> TopicPathPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "services", "service", "solutions", "solution", "products", "product",
        "industries", "industry", "verticals", "vertical", "offerings", "offering",
        "specialties", "specialty", "expertise", "capabilities", "practice-areas",
        "areas", "locations", "markets",
    };

    public UrlPatternData Extract(IReadOnlyList<string> absoluteUrls, string siteUrl)
    {
        if (absoluteUrls.Count == 0)
            return new UrlPatternData([], 0);

        if (!TryGetOrigin(siteUrl, out var origin))
            return new UrlPatternData([], absoluteUrls.Count);

        var bySlug = new Dictionary<string, UrlPatternTopic>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in absoluteUrls.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!url.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = url[origin.Length..].TrimStart('/');
            if (string.IsNullOrWhiteSpace(relative))
                continue;

            foreach (var (segment, topicSlug) in ExtractTopicSegments(relative))
            {
                if (NoisePaths.IsNoise(topicSlug))
                    continue;

                if (bySlug.ContainsKey(topicSlug))
                    continue;

                bySlug[topicSlug] = new UrlPatternTopic(
                    SitemapExtractor.SlugToTitle(topicSlug),
                    topicSlug,
                    url,
                    segment);
            }
        }

        return new UrlPatternData(
            bySlug.Values.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            absoluteUrls.Count);
    }

    internal static IEnumerable<(string Segment, string TopicSlug)> ExtractTopicSegments(string relativePath)
    {
        var segments = relativePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split('?', '#')[0])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (segments.Count == 0)
            yield break;

        if (segments.Count >= 2 && TopicPathPrefixes.Contains(segments[0]))
        {
            var topicSlug = segments[1];
            if (!NoisePaths.IsNoise(topicSlug))
                yield return (segments[1], topicSlug);
            yield break;
        }

        var last = segments[^1];
        if (!NoisePaths.IsNoise(last) && last.Length >= 3)
            yield return (last, last);
    }

    private static bool TryGetOrigin(string siteUrl, out string origin)
    {
        origin = string.Empty;
        try
        {
            origin = new Uri(NicheSiteUrlNormalizer.Normalize(siteUrl)).GetLeftPart(UriPartial.Authority);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
