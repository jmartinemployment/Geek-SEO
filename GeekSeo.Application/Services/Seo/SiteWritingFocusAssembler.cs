using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Services.Seo;

/// <summary>
/// Legacy site focus assembly from Niche Analyzer + projects. Not used when SA2 handoff bundles are frozen.
/// </summary>
public sealed class SiteWritingFocusAssembler(
    IProjectRepository projects,
    INicheProfileRepository nicheProfiles,
    INicheAnalyticsDapperRepository nicheAnalytics,
    ISiteResearchRepository siteResearch)
{
    public async Task<SiteWritingFocus> AssembleLegacyAsync(
        Guid userId,
        Guid projectId,
        string articleKeyword,
        string searchLocation,
        string? serpKeyword = null,
        CancellationToken ct = default)
    {
        var capturedAt = DateTimeOffset.UtcNow;
        var keyword = string.IsNullOrWhiteSpace(articleKeyword) ? string.Empty : articleKeyword.Trim();
        var location = string.IsNullOrWhiteSpace(searchLocation) ? "United States" : searchLocation.Trim();

        var projectResult = await projects.GetByIdAsync(projectId, ct);
        if (!projectResult.IsSuccess || projectResult.Value is null)
        {
            return new SiteWritingFocus
            {
                SiteName = "Unknown site",
                SiteUrl = string.Empty,
                CapturedAt = capturedAt,
            };
        }

        var project = projectResult.Value;

        var profileTask = nicheProfiles.GetLatestByProjectAsync(projectId, ct);
        var siteResearchTask = siteResearch.GetOrCreateForProjectAsync(
            userId,
            new CreateSiteResearchRequest { ProjectId = projectId, SiteUrl = project.Url },
            ct);

        await Task.WhenAll(profileTask, siteResearchTask);

        var profile = profileTask.Result.IsSuccess ? profileTask.Result.Value : null;
        var businessSummary = siteResearchTask.Result.IsSuccess
            ? siteResearchTask.Result.Value?.BusinessSummary ?? string.Empty
            : string.Empty;

        var matchedPillar = SiteWritingFocusHelpers.FindMatchedPillar(keyword, profile);
        var gapTopics = await TryGetGapTopicsAsync(profile?.Id, ct);
        var geoNodes = SiteWritingFocusHelpers.BuildGeoAnchorNodes(
            location,
            project.BusinessAddress,
            project.DefaultLocation);

        var competitorDomains = profile?.Competitors
            .Select(c => c.Domain)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList() ?? [];

        var authorityPages = profile?.Pillars
            .Where(p => !string.IsNullOrWhiteSpace(p.PageUrl))
            .Select(p => p.PageUrl!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList() ?? [];

        var focus = new SiteWritingFocus
        {
            SiteName = project.Name,
            SiteUrl = project.Url,
            PrimaryNiche = profile?.PrimaryNiche ?? string.Empty,
            NicheDescription = profile?.NicheDescription ?? string.Empty,
            NicheTags = profile?.NicheTags ?? [],
            BusinessSummary = businessSummary,
            MatchedPillarTopic = matchedPillar?.PillarTopic,
            MatchedPillarIntent = matchedPillar?.SearchIntent,
            MatchedPillarAngle = matchedPillar?.ContentAngle,
            GeoAnchorNodes = geoNodes,
            ServiceAreaDescription = SiteWritingFocusHelpers.BuildServiceAreaDescription(project),
            GapTopics = gapTopics,
            CompetitorDomains = competitorDomains,
            AuthorityPageUrls = authorityPages,
            NicheProfileId = profile?.Id,
            NicheProfileUpdatedAt = profile?.AnalyzedAt ?? profile?.CreatedAt,
            CapturedAt = capturedAt,
        };

        var writingInstructions = SiteWritingFocusHelpers.BuildHeuristicWritingInstructions(
            focus,
            keyword,
            serpKeyword);

        return focus with { WritingInstructions = writingInstructions.Trim() };
    }

    private async Task<IReadOnlyList<string>> TryGetGapTopicsAsync(Guid? profileId, CancellationToken ct)
    {
        if (profileId is null)
            return [];

        var gaps = await nicheAnalytics.GetTopicalGapsAsync(profileId.Value, quickWinsOnly: false, ct);
        return gaps.IsSuccess && gaps.Value is not null
            ? gaps.Value.Select(g => g.SubtopicTitle).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList()
            : [];
    }
}
