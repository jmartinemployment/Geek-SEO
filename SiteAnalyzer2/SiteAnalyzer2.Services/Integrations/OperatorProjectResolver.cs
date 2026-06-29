using SiteAnalyzer2.Domain.Entities;

namespace SiteAnalyzer2.Services.Integrations;

/// <summary>
/// Back-compat wrapper — use <see cref="SiteProfileService"/> for import resolution.
/// </summary>
public sealed class OperatorProjectResolver(SiteProfileService siteProfiles)
{
    public Task<Guid> ResolveProjectIdAsync(string targetSiteUrl, CancellationToken ct = default) =>
        siteProfiles.ResolveProjectIdForImportAsync(targetSiteUrl, ct);

    public async Task<SiteProfile> ResolveSiteProfileAsync(string targetSiteUrl, CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        if (string.IsNullOrEmpty(normalized))
            throw new InvalidOperationException("Project URL is required.");

        var profile = await siteProfiles.FindByUrlAsync(normalized, ct);
        if (profile is not null)
            return profile;

        throw new InvalidOperationException(
            $"No site profile exists for {normalized}. " +
            "Parse a keyword page to create one, or register the site in Geek-SEO first.");
    }

    public Task LinkGeekSeoProjectAsync(Guid geekSeoProjectId, string targetSiteUrl, CancellationToken ct = default) =>
        siteProfiles.LinkGeekSeoProjectAsync(geekSeoProjectId, targetSiteUrl, ct);
}
