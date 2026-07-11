using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;

namespace GeekSeo.Application.Services.Seo;

internal static class SiteWritingFocusHelpers
{
    public static string? FindMatchedPillarTopic(string keyword, NicheProfile? profile)
    {
        var pillar = FindMatchedPillar(keyword, profile);
        return pillar?.PillarTopic;
    }

    public static NichePillar? FindMatchedPillar(string keyword, NicheProfile? profile)
    {
        if (profile?.Pillars is null || profile.Pillars.Count == 0)
            return null;

        var normalizedKeyword = keyword.ToLowerInvariant();
        var direct = profile.Pillars.FirstOrDefault(p =>
            normalizedKeyword.Contains(p.PillarTopic, StringComparison.OrdinalIgnoreCase)
            || normalizedKeyword.Contains(p.PrimaryKeyword, StringComparison.OrdinalIgnoreCase)
            || p.PrimaryKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(token => normalizedKeyword.Contains(token, StringComparison.OrdinalIgnoreCase)));

        return direct ?? profile.Pillars.OrderBy(p => p.DisplayOrder).FirstOrDefault();
    }

    public static IReadOnlyList<string> BuildGeoAnchorNodes(
        string location,
        string? businessAddress,
        string? defaultLocation) =>
        new[] { location, businessAddress, defaultLocation }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string BuildServiceAreaDescription(SeoProject project)
    {
        if (!project.LocalSeoEnabled)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(project.BusinessAddress))
        {
            var radius = project.ServiceRadiusMiles > 0 ? project.ServiceRadiusMiles : 20;
            return $"Serves customers within {radius} miles of {project.BusinessAddress.Trim()}.";
        }

        if (!string.IsNullOrWhiteSpace(project.DefaultLocation)
            && !string.Equals(project.DefaultLocation, "United States", StringComparison.OrdinalIgnoreCase))
            return $"Primary market: {project.DefaultLocation.Trim()}.";

        return string.Empty;
    }

    public static string BuildHeuristicWritingInstructions(
        SiteWritingFocus focus,
        string articleKeyword,
        string? serpKeyword)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(focus.BusinessSummary))
            lines.Add($"Business: {focus.BusinessSummary.Trim()}");

        if (!string.IsNullOrWhiteSpace(focus.PrimaryNiche))
            lines.Add($"Site niche: {focus.PrimaryNiche.Trim()}.");

        if (!string.IsNullOrWhiteSpace(focus.NicheDescription))
            lines.Add(focus.NicheDescription.Trim());

        if (focus.NicheTags.Count > 0)
            lines.Add($"Themes: {string.Join(", ", focus.NicheTags.Take(8))}.");

        if (!string.IsNullOrWhiteSpace(focus.MatchedPillarTopic))
        {
            var pillarLine = $"Align with pillar \"{focus.MatchedPillarTopic}\"";
            if (!string.IsNullOrWhiteSpace(focus.MatchedPillarIntent))
                pillarLine += $" ({focus.MatchedPillarIntent})";
            if (!string.IsNullOrWhiteSpace(focus.MatchedPillarAngle))
                pillarLine += $". Angle: {focus.MatchedPillarAngle}";
            lines.Add(pillarLine + ".");
        }

        if (focus.GeoAnchorNodes.Count > 0)
            lines.Add($"Geo context: {string.Join("; ", focus.GeoAnchorNodes.Take(4))}.");

        if (!string.IsNullOrWhiteSpace(focus.ServiceAreaDescription))
            lines.Add(focus.ServiceAreaDescription);

        if (focus.GapTopics.Count > 0)
            lines.Add($"Content gaps to reinforce: {string.Join(", ", focus.GapTopics)}.");

        if (!string.IsNullOrWhiteSpace(serpKeyword)
            && !string.Equals(articleKeyword, serpKeyword, StringComparison.OrdinalIgnoreCase))
        {
            lines.Add(
                $"Article keyword is \"{articleKeyword}\" but SERP research used \"{serpKeyword}\" — write for the article keyword while matching the SERP patterns.");
        }

        return string.Join(" ", lines).Trim();
    }
}
