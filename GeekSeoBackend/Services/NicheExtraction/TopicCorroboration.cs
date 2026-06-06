using GeekSeo.Application.Models.Seo;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Engine-like corroboration: weak single-source signals need a second independent family.
/// </summary>
internal static class TopicCorroboration
{
    internal static bool PassesCorroboration(IReadOnlyList<TopicEvidence> evidence)
    {
        var sources = evidence
            .Select(e => e.Source)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (sources.Contains("schema") || sources.Contains("same_as"))
            return true;

        if (sources.Contains("page_vertical"))
            return true;

        if (sources.Contains("sitemap") || sources.Contains("internal_link") || sources.Contains("url_pattern"))
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
