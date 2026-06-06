using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>Collects peer-level topic candidates from all Tier-1 extractors.</summary>
internal static class TopicCandidatePoolBuilder
{
    internal static IReadOnlyList<TopicCandidate> Build(
        SchemaOrgData schema,
        SitemapData sitemap,
        NavMenuData nav,
        HomepageHeadings headings,
        PageContentData pageContent,
        InternalLinkData? internalLinks = null,
        UrlPatternData? urlPatterns = null)
    {
        var bySlug = new Dictionary<string, TopicCandidateBuilder>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in schema.KnowsAboutTopics)
            AddEvidence(bySlug, name, "schema", TopicEvidenceWeights.Schema, "knowsAbout");

        foreach (var name in schema.OfferCatalogTopics)
            AddEvidence(bySlug, name, "schema", TopicEvidenceWeights.Schema, "offerCatalog/serviceType");

        foreach (var pillar in sitemap.Pillars)
        {
            AddEvidence(
                bySlug,
                pillar.Name,
                "sitemap",
                TopicEvidenceWeights.Sitemap,
                pillar.PageUrl ?? pillar.Slug,
                pillar.PageUrl);
        }

        foreach (var pillar in nav.Pillars)
        {
            AddEvidence(
                bySlug,
                pillar.Name,
                "nav",
                TopicEvidenceWeights.Nav,
                nav.ExtractMethod,
                pillar.PageUrl);
        }

        foreach (var pillar in HeadingPillarBuilder.Build(headings))
        {
            AddEvidence(
                bySlug,
                pillar.Name,
                "heading",
                TopicEvidenceWeights.Heading,
                "homepage heading");
        }

        foreach (var phrase in pageContent.ServicePhrases)
            AddEvidence(bySlug, phrase, "page", TopicEvidenceWeights.Page, "homepage body");

        foreach (var vertical in pageContent.VerticalTopics)
            AddEvidence(bySlug, vertical, "page_vertical", TopicEvidenceWeights.PageVertical, "homepage H2/H3 vertical section");

        if (schema.EntityResolved)
        {
            var platformSnippet = string.Join(", ", schema.ResolvedEntityPlatforms);
            foreach (var builder in bySlug.Values.ToList())
            {
                if (!builder.HasSource("schema"))
                    continue;

                builder.AddEvidence(
                    "same_as",
                    TopicEvidenceWeights.SameAs,
                    platformSnippet,
                    schema.SameAsUrls.FirstOrDefault());
            }
        }

        if (internalLinks is not null)
        {
            foreach (var edge in internalLinks.Links)
            {
                AddEvidence(
                    bySlug,
                    edge.AnchorText,
                    "internal_link",
                    TopicEvidenceWeights.InternalLink,
                    edge.InferredFromUrlSlug
                        ? $"URL slug: {edge.TargetUrl}"
                        : edge.AnchorText,
                    edge.TargetUrl,
                    trackInbound: true);
            }
        }

        if (urlPatterns is not null)
        {
            foreach (var topic in urlPatterns.Topics)
            {
                AddEvidence(
                    bySlug,
                    topic.Name,
                    "url_pattern",
                    TopicEvidenceWeights.UrlPattern,
                    topic.PathSegment,
                    topic.Url);
            }
        }

        if (internalLinks is not null)
        {
            foreach (var builder in bySlug.Values)
                builder.ApplyInboundCounts(internalLinks.InboundCountByTargetUrl);
        }

        return bySlug.Values
            .Select(b => b.ToCandidate())
            .OrderByDescending(c => c.Confidence)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddEvidence(
        Dictionary<string, TopicCandidateBuilder> bySlug,
        string name,
        string source,
        decimal weight,
        string? snippet,
        string? url = null,
        bool trackInbound = false)
    {
        var trimmed = name.Trim();
        if (trimmed.Length < 3)
            return;

        var slug = NicheAnalyzerService.NameToSlug(trimmed);
        if (string.IsNullOrWhiteSpace(slug) || NoisePaths.IsNoise(slug))
            return;

        if (!bySlug.TryGetValue(slug, out var builder))
        {
            builder = new TopicCandidateBuilder(trimmed, slug);
            bySlug[slug] = builder;
        }

        builder.AddEvidence(source, weight, snippet, url, trackInbound);
    }

    private sealed class TopicCandidateBuilder(string name, string slug)
    {
        private readonly List<TopicEvidence> _evidence = [];
        private readonly HashSet<string> _inboundTargets = new(StringComparer.OrdinalIgnoreCase);
        private string? _pageUrl;
        private int _inboundLinkCount;

        internal void AddEvidence(
            string source,
            decimal weight,
            string? snippet,
            string? url,
            bool trackInbound = false)
        {
            if (url is not null && _pageUrl is null && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                _pageUrl = url;

            if (trackInbound && url is not null)
                _inboundTargets.Add(url);

            _evidence.Add(new TopicEvidence
            {
                Source = source,
                Weight = weight,
                Snippet = snippet,
                Url = url,
            });
        }

        internal bool HasSource(string source) =>
            _evidence.Any(e => e.Source.Equals(source, StringComparison.OrdinalIgnoreCase));

        internal void ApplyInboundCounts(IReadOnlyDictionary<string, int> inboundByTarget)
        {
            foreach (var target in _inboundTargets)
            {
                if (inboundByTarget.TryGetValue(target, out var count))
                    _inboundLinkCount += count;
            }

            if (_pageUrl is not null && inboundByTarget.TryGetValue(_pageUrl, out var pageInbound))
                _inboundLinkCount = Math.Max(_inboundLinkCount, pageInbound);
        }

        internal TopicCandidate ToCandidate()
        {
            var confidence = Math.Min(
                TopicEvidenceWeights.MaxConfidence,
                _evidence.Sum(e => e.Weight));

            var linkSignals = _evidence.Count(e =>
                e.Source is "internal_link" or "nav" or "url_pattern");
            var internalLinkCount = Math.Max(_inboundLinkCount, linkSignals);

            return new TopicCandidate
            {
                Name = name,
                Slug = slug,
                Evidence = _evidence,
                Confidence = confidence,
                DedicatedPageUrl = _pageUrl,
                InternalLinkCount = internalLinkCount,
            };
        }
    }
}
