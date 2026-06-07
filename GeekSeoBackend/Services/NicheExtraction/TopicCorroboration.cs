using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Engine-like corroboration mirrors how search engines verify topical authority:
/// structural signals (URL structure, links) alone are insufficient — they must be
/// backed by on-page content signals or by schema with corroborating evidence.
/// </summary>
internal static class TopicCorroboration
{
    internal static bool PassesCorroboration(IReadOnlyList<TopicEvidence> evidence, decimal confidence)
    {
        var sources = evidence
            .Select(e => e.Source)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Schema/sameAs: trusted only when confidence indicates at least one corroborating signal.
        // Schema alone = 0.35, which falls below the floor — any additional signal pushes above it.
        if (sources.Contains("schema") || sources.Contains("same_as"))
            return confidence >= TopicEvidenceWeights.SchemaConfidenceFloor;

        // page_vertical = dedicated content section; strong on-page signal, auto-pass.
        if (sources.Contains("page_vertical"))
            return true;

        // Structural signals (sitemap URL, internal link anchor, URL slug) confirm site architecture
        // but not content authority. Require at least one content-side signal alongside them.
        var hasContentSignal = sources.Any(s => s is "page" or "heading");
        var hasStructuralSignal = sources.Any(s => s is "sitemap" or "internal_link" or "url_pattern");
        if (hasStructuralSignal && hasContentSignal)
            return true;

        // GSC queries are real search demand evidence; treat as a content-equivalent signal.
        if (sources.Contains("gsc") && (hasStructuralSignal || hasContentSignal))
            return true;

        return CountIndependentFamilies(sources) >= 2;
    }

    internal static int CountIndependentFamilies(IReadOnlyCollection<string> sources)
    {
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            var family = source switch
            {
                "page" or "page_vertical" => "page",
                _ => source,
            };
            families.Add(family);
        }

        return families.Count;
    }
}
