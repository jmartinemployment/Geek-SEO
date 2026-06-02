using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.NicheExtraction;

/// <summary>
/// Converts homepage H1–H6 into pillar candidates (used when higher-priority sources are thin).
/// </summary>
internal static class HeadingPillarBuilder
{
    public static IReadOnlyList<DiscoveredPillar> Build(HomepageHeadings headings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pillars = new List<DiscoveredPillar>();

        foreach (var heading in headings.Headings)
        {
            var text = heading.Text.Trim();
            if (text.Length < 4)
                continue;

            if (NoisePaths.H2Noise.Contains(text))
                continue;

            var slug = NicheAnalyzerService.NameToSlug(text);
            if (string.IsNullOrWhiteSpace(slug) || NoisePaths.IsNoise(slug))
                continue;

            if (!seen.Add(slug))
                continue;

            pillars.Add(new DiscoveredPillar
            {
                Name = text,
                Slug = slug,
                Intent = InferIntent(heading.Level),
                Source = "heading",
                ChildPageCount = 1,
            });
        }

        return pillars;
    }

    private static string InferIntent(int level) => level switch
    {
        1 or 2 => "commercial",
        3 => "commercial",
        _ => "informational",
    };
}
