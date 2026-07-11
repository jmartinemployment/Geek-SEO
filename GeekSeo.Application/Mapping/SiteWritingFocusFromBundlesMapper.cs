using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Services.Seo;

namespace GeekSeo.Application.Mapping;

/// <summary>
/// Builds frozen <see cref="SiteWritingFocus"/> from SA2 site + keyword bundles only (no Niche Analyzer).
/// Keyword-specific pillar/gap fields come from the keyword bundle.
/// </summary>
public static class SiteWritingFocusFromBundlesMapper
{
    public static SiteWritingFocus Map(
        ContentWriterSiteBundle site,
        ContentWriterSerpExport keyword,
        string articleKeyword)
    {
        var capturedAt = DateTimeOffset.UtcNow;
        var keywordLabel = string.IsNullOrWhiteSpace(articleKeyword) ? keyword.Keyword : articleKeyword.Trim();

        var focus = new SiteWritingFocus
        {
            SiteProfileId = site.SiteProfileId,
            SiteName = !string.IsNullOrWhiteSpace(site.DisplayName)
                ? site.DisplayName.Trim()
                : site.SiteUrl,
            SiteUrl = site.SiteUrl.Trim(),
            PrimaryNiche = site.PrimaryNiche ?? string.Empty,
            NicheDescription = site.NicheDescription ?? string.Empty,
            NicheTags = site.NicheTags,
            BusinessSummary = site.BusinessSummary?.Trim() ?? string.Empty,
            MatchedPillarTopic = keyword.MatchedPillarTopic,
            MatchedPillarIntent = keyword.MatchedPillarIntent,
            MatchedPillarAngle = keyword.MatchedPillarAngle,
            GeoAnchorNodes = site.GeoAnchorNodes,
            ServiceAreaDescription = site.ServiceAreaDescription?.Trim() ?? string.Empty,
            GapTopics = keyword.GapTopics,
            CompetitorDomains = site.CompetitorDomains.Take(8).ToList(),
            AuthorityPageUrls = site.AuthorityPageUrls.Take(8).ToList(),
            CapturedAt = capturedAt,
        };

        var writingInstructions = BuildWritingInstructions(site, keyword, keywordLabel, focus);
        return focus with { WritingInstructions = writingInstructions };
    }

    private static string BuildWritingInstructions(
        ContentWriterSiteBundle site,
        ContentWriterSerpExport keyword,
        string articleKeyword,
        SiteWritingFocus focus)
    {
        if (!string.IsNullOrWhiteSpace(keyword.WritingInstructions))
            return keyword.WritingInstructions.Trim();

        var heuristic = SiteWritingFocusHelpers.BuildHeuristicWritingInstructions(
            focus,
            articleKeyword,
            keyword.Keyword);

        var siteRecs = site.WritingRecommendations
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Take(6)
            .ToList();

        var keywordRecs = keyword.WritingRecommendations
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Take(6)
            .ToList();

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(heuristic))
            parts.Add(heuristic.Trim());

        if (siteRecs.Count > 0)
            parts.Add(string.Join(" ", siteRecs));

        if (keywordRecs.Count > 0)
            parts.Add(string.Join(" ", keywordRecs));

        return string.Join(" ", parts).Trim();
    }
}
