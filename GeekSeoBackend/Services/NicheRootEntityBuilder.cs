using GeekSeo.Application.Models.Seo;
using GeekSeo.Persistence.Entities;

namespace GeekSeoBackend.Services;

public sealed class NicheRootEntityBuilder
{
    public string Build(
        SchemaOrgData schema,
        HomepageHeadings headings,
        IReadOnlyList<NichePillar> pillars)
    {
        var parts = new List<string>();

        // Brand / niche from schema brand name or title
        var brand = schema.BrandName ?? ExtractTitleCore(headings.Title);
        if (!string.IsNullOrWhiteSpace(brand)) parts.Add(brand);

        // Top 2 pillar topics (must_have + high_value first)
        var topPillars = pillars
            .OrderBy(p => p.StrategicPriority switch
            {
                "must_have" => 0, "high_value" => 1, _ => 2,
            })
            .Take(2)
            .Select(p => p.PillarTopic)
            .ToList();

        if (topPillars.Count > 0 && !parts.Any(p =>
            p.Contains(topPillars[0], StringComparison.OrdinalIgnoreCase)))
        {
            parts.Add(topPillars[0]);
        }

        // Location from areaServed
        var location = schema.AreaServed.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(location) &&
            !parts.Any(p => p.Contains(location, StringComparison.OrdinalIgnoreCase)))
        {
            parts.Add(location);
        }

        if (parts.Count == 0) return "Unknown Niche";
        return string.Join(", ", parts);
    }

    private static string? ExtractTitleCore(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        // Split on | or – and take the longest meaningful segment
        var segments = title.Split(['|', '–', '-', '—'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 3)
            .OrderByDescending(s => s.Length)
            .ToList();

        return segments.FirstOrDefault();
    }
}
