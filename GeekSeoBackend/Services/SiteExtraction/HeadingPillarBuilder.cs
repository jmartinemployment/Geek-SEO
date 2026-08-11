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
    // Site Structure pillar list disabled per product direction — if heading its valid but list not needed, return empty.
    public static IReadOnlyList<DiscoveredPillar> Build(IEnumerable<PageHeading> headings) => [];
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
