using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Services.ProfileAssembly;

namespace SiteAnalyzer2.Services.Integrations;

public static class ContentWriterSiteBundleBuilder
{
    public static ContentWriterSiteBundleDto Build(SiteProfile profile, DateTimeOffset capturedAt) =>
        new()
        {
            BundleVersion = ContentWriterSiteBundleDto.CurrentBundleVersion,
            CapturedAt = capturedAt,
            SiteProfileId = profile.Id,
            GeekSeoProjectId = profile.GeekSeoProjectId,
            SiteUrl = profile.SiteUrl,
            DisplayName = profile.DisplayName,
            CreatedAt = ToOffset(profile.CreatedAt),
            UpdatedAt = ToOffset(profile.UpdatedAt),
            BusinessProfileAt = ToOffset(profile.BusinessProfileAt),
            LastRunAt = ToOffset(profile.LastRunAt),
            BusinessType = profile.BusinessType,
            BusinessDescription = profile.BusinessDescription,
            BusinessSummary = profile.BusinessSummary,
            GeneratedSchemaJson = string.IsNullOrWhiteSpace(profile.GeneratedSchemaJson)
                ? null
                : profile.GeneratedSchemaJson.Trim(),
            PrimaryNiche = profile.PrimaryNiche,
            NicheDescription = profile.NicheDescription,
            NicheTags = profile.NicheTags.ToList(),
            GeoAnchorNodes = profile.GeoAnchorNodes.ToList(),
            ServiceAreaDescription = profile.ServiceAreaDescription,
            CompetitorDomains = profile.CompetitorDomains.ToList(),
            AuthorityPageUrls = profile.AuthorityPageUrls.ToList(),
            WritingRecommendations = profile.WritingRecommendations.ToList(),
            RecommendedHomepageJsonLd = HomepageJsonLdRecommendationBuilder.Build(profile)
                .Select(s => new RecommendedJsonLdSnippetDto(s.Id, s.Title, s.Description, s.Json, s.ScriptTag))
                .ToList(),
        };

    private static DateTimeOffset ToOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value is null ? null : ToOffset(value.Value);
}
