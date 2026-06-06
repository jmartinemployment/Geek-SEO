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
        PageContentData pageContent)
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
            AddEvidence(bySlug, vertical, "page_vertical", TopicEvidenceWeights.PageVertical, "homepage H3 section");

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
        string? url = null)
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

        builder.AddEvidence(source, weight, snippet, url);
    }

    private sealed class TopicCandidateBuilder(string name, string slug)
    {
        private readonly List<TopicEvidence> _evidence = [];
        private string? _pageUrl;

        internal void AddEvidence(string source, decimal weight, string? snippet, string? url)
        {
            if (url is not null && _pageUrl is null && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                _pageUrl = url;

            _evidence.Add(new TopicEvidence
            {
                Source = source,
                Weight = weight,
                Snippet = snippet,
                Url = url,
            });
        }

        internal TopicCandidate ToCandidate()
        {
            var confidence = Math.Min(
                TopicEvidenceWeights.MaxConfidence,
                _evidence.Sum(e => e.Weight));

            return new TopicCandidate
            {
                Name = name,
                Slug = slug,
                Evidence = _evidence,
                Confidence = confidence,
                DedicatedPageUrl = _pageUrl,
                InternalLinkCount = _evidence.Count(e => e.Source == "nav"),
            };
        }
    }
}
