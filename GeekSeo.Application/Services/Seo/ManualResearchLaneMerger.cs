using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

public static class ManualResearchLaneMerger
{
    public static ContentWriterSerpExport Merge(ContentWriterSerpExport export)
    {
        if (export.ManualResearchLanes.Count == 0)
            return export;

        var citations = new List<ContentWriterCitationCandidate>(export.CitationCandidates);
        var seen = new HashSet<string>(citations.Select(c => c.Url.Trim()), StringComparer.OrdinalIgnoreCase);
        string? localAngle = export.LocalAngleHint;

        foreach (var lane in export.ManualResearchLanes)
        {
            if (lane.OrganicCount == 0)
                continue;

            if (string.Equals(lane.Lane, SerpResearchLanes.Local, StringComparison.OrdinalIgnoreCase))
            {
                localAngle = BuildLocalAngle(lane) ?? localAngle;
                continue;
            }

            foreach (var organic in lane.OrganicResults)
            {
                if (string.IsNullOrWhiteSpace(organic.Url))
                    continue;

                var url = organic.Url.Trim();
                if (!seen.Add(url))
                    continue;

                var source = CitationDomainSourceResolver.Resolve(url);
                if (string.Equals(source, "unknown", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!AuthoritativeCitationRules.IsAcceptableDiscoveredCitationUrl(url)
                    && !AuthoritativeCitationRules.IsAuthoritativeCitationUrl(url))
                    continue;

                citations.Add(new ContentWriterCitationCandidate
                {
                    Url = url,
                    Title = organic.Title,
                    Domain = organic.Domain,
                    Source = source,
                });
            }
        }

        return export with
        {
            CitationCandidates = citations,
            LocalAngleHint = localAngle,
        };
    }

    private static string? BuildLocalAngle(ContentWriterManualResearchLane lane)
    {
        var snippets = lane.OrganicResults
            .Select(o => o.Snippet)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Take(2)
            .ToList();

        return snippets.Count == 0 ? null : string.Join(" ", snippets).Trim();
    }

    public static bool HasNonEmptyManualLane(
        IReadOnlyList<ContentWriterManualResearchLane> lanes,
        string bucket) =>
        bucket switch
        {
            "citations_wikipedia" => LaneHasResults(lanes, SerpResearchLanes.Wiki),
            "citations_government" => LaneHasResults(lanes, SerpResearchLanes.Gov),
            "citations_research" or "citations_pdf" or "scholar" => LaneHasResults(lanes, SerpResearchLanes.Edu),
            "local_angle" => LaneHasResults(lanes, SerpResearchLanes.Local),
            _ => false,
        };

    private static bool LaneHasResults(IReadOnlyList<ContentWriterManualResearchLane> lanes, string lane) =>
        lanes.Any(l => string.Equals(l.Lane, lane, StringComparison.OrdinalIgnoreCase) && l.OrganicCount > 0);
}
