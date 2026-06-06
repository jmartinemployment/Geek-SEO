using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Builds pillar-to-pillar internal link graph from crawled anchor edges (Gap 5).
/// </summary>
internal static class InternalLinkGraphBuilder
{
    internal static InternalLinkGraph Build(
        FusedSiteUnderstanding fused,
        InternalLinkData internalLinks,
        UrlPatternData urlPatterns)
    {
        var slugByUrl = BuildUrlSlugIndex(fused, urlPatterns);
        var selectedSlugs = fused.SelectedPillars
            .Select(p => p.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selectedSlugs.Count == 0 || internalLinks.Links.Count == 0)
        {
            return new InternalLinkGraph([], selectedSlugs.OrderBy(s => s).ToList());
        }

        var edgeMap = new Dictionary<(string From, string To), LinkAccumulator>();

        foreach (var link in internalLinks.Links)
        {
            if (!TryResolveSlug(link.SourceUrl, slugByUrl, out var fromSlug))
                continue;
            if (!TryResolveSlug(link.TargetUrl, slugByUrl, out var toSlug))
                continue;
            if (!selectedSlugs.Contains(fromSlug) || !selectedSlugs.Contains(toSlug))
                continue;
            if (fromSlug.Equals(toSlug, StringComparison.OrdinalIgnoreCase))
                continue;

            var key = (fromSlug, toSlug);
            if (!edgeMap.TryGetValue(key, out var acc))
            {
                acc = new LinkAccumulator();
                edgeMap[key] = acc;
            }

            acc.Count++;
            if (acc.Anchors.Count < 3 && !string.IsNullOrWhiteSpace(link.AnchorText))
                acc.Anchors.Add(link.AnchorText);
        }

        var edges = edgeMap
            .Select(kvp => new InternalLinkGraphEdge(
                kvp.Key.From,
                kvp.Key.To,
                kvp.Value.Count,
                kvp.Value.Anchors.ToList()))
            .OrderByDescending(e => e.LinkCount)
            .ThenBy(e => e.FromSlug, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var connected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            connected.Add(edge.FromSlug);
            connected.Add(edge.ToSlug);
        }

        var orphans = selectedSlugs
            .Where(s => !connected.Contains(s))
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new InternalLinkGraph(edges, orphans);
    }

    private static Dictionary<string, string> BuildUrlSlugIndex(
        FusedSiteUnderstanding fused,
        UrlPatternData urlPatterns)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pillar in fused.AllCandidates)
        {
            if (!string.IsNullOrWhiteSpace(pillar.DedicatedPageUrl))
                map[NormalizeUrlKey(pillar.DedicatedPageUrl)] = pillar.Slug;
        }

        foreach (var topic in urlPatterns.Topics)
            map[NormalizeUrlKey(topic.Url)] = topic.Slug;

        return map;
    }

    private static bool TryResolveSlug(
        string url,
        IReadOnlyDictionary<string, string> slugByUrl,
        out string slug)
    {
        slug = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var key = NormalizeUrlKey(url);
        if (slugByUrl.TryGetValue(key, out slug))
            return true;

        try
        {
            var path = new Uri(url).AbsolutePath.Trim('/');
            foreach (var (_, topicSlug) in UrlPatternExtractor.ExtractTopicSegments(path))
            {
                if (NoisePaths.IsNoise(topicSlug))
                    continue;

                slug = topicSlug;
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static string NormalizeUrlKey(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
        }
        catch
        {
            return url.TrimEnd('/').ToLowerInvariant();
        }
    }

    private sealed class LinkAccumulator
    {
        internal int Count { get; set; }
        internal List<string> Anchors { get; } = [];
    }
}
