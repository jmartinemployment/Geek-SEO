using GeekSeo.Application.Models.Seo;
using GeekSeoBackend.Services;

namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Converts page headings (H1–H6) from any crawled page into pillar candidates.
/// Callers should pass the full persisted heading set (every crawled page after site crawl),
/// not homepage-only. Noise/slug filters still drop chrome and empty/short headings.
/// </summary>
internal static class HeadingPillarBuilder
{
    public static IReadOnlyList<DiscoveredPillar> Build(IEnumerable<PageHeading> headings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pillars = new List<DiscoveredPillar>();

        foreach (var heading in headings.Where(h => h.Level is >= 1 and <= 6))
        {
            var text = heading.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var slug = SiteAnalyzerService.NameToSlug(text);
            if (string.IsNullOrWhiteSpace(slug))
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

    /// <summary>Convenience overload when headings are wrapped in <see cref="HomepageHeadings"/> (may be site-wide after crawl).</summary>
    public static IReadOnlyList<DiscoveredPillar> Build(HomepageHeadings headings) =>
        Build(headings.Headings);

    private static string InferIntent(int level) => level switch
    {
        1 or 2 => "commercial",
        3 => "commercial",
        _ => "informational",
    };
}
