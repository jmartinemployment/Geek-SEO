using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Domain.Entities;
using SiteAnalyzer2.Infrastructure.Persistence;
using SiteAnalyzer2.Services.ProfileAssembly;

namespace SiteAnalyzer2.Services.Integrations;

public sealed record CreateSiteProfileResult(
    Guid Id,
    string SiteUrl,
    string DisplayName,
    bool Created);

public sealed record SiteProfileDto(
    Guid Id,
    string SiteUrl,
    Guid? GeekSeoProjectId,
    string? DisplayName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? BusinessProfileAt,
    DateTime? LastRunAt,
    string? BusinessType,
    string? BusinessDescription,
    string? BusinessSummary,
    string? PrimaryNiche,
    string? NicheDescription,
    IReadOnlyList<string> NicheTags,
    IReadOnlyList<string> GeoAnchorNodes,
    string? ServiceAreaDescription,
    IReadOnlyList<string> CompetitorDomains,
    IReadOnlyList<string> AuthorityPageUrls,
    IReadOnlyList<string> WritingRecommendations,
    IReadOnlyList<RecommendedJsonLdSnippetDto> RecommendedHomepageJsonLd);

public sealed record RecommendedJsonLdSnippetDto(
    string Id,
    string Title,
    string Description,
    string Json,
    string ScriptTag);

/// <summary>
/// Project URL → <c>site_profiles</c> (unique <c>site_url</c>) → Geek-SEO <c>projects.Id</c>.
/// </summary>
public sealed class SiteProfileService(AppDbContext db)
{
    public async Task<Guid> ResolveProjectIdForImportAsync(string targetSiteUrl, CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        if (string.IsNullOrEmpty(normalized))
            throw new InvalidOperationException("Project URL is required.");

        if (OperatorBootstrapConfiguration.TryResolveGeekSeoProjectId() is Guid bootstrapId)
        {
            await LinkBootstrapGeekSeoProjectAsync(bootstrapId, normalized, ct);
            return bootstrapId;
        }

        var profile = await FindByUrlAsync(normalized, ct);
        if (profile?.GeekSeoProjectId is Guid linked && linked != Guid.Empty)
            return linked;

        throw new InvalidOperationException(
            $"Geek-SEO project is not linked for {normalized}. " +
            "Set GEEK_SEO_PROJECT_ID on the Site Analyzer Api service to your Content Writer project id.");
    }

    public async Task<SiteProfile?> FindByUrlAsync(string normalizedUrl, CancellationToken ct = default) =>
        await db.SiteProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SiteUrl == normalizedUrl, ct);

    public async Task<SiteProfileDto?> GetDetailByUrlAsync(string siteUrl, CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(siteUrl);
        if (string.IsNullOrEmpty(normalized))
            return null;

        var profile = await FindByUrlAsync(normalized, ct);
        return profile is null ? null : ToDto(profile);
    }

    public async Task<CreateSiteProfileResult> CreateOrGetAsync(
        string siteUrl,
        string? displayName = null,
        CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(siteUrl);
        if (string.IsNullOrEmpty(normalized) || !TargetSiteUrlNormalizer.IsValidStoredFormat(normalized))
        {
            throw new InvalidOperationException(
                "Invalid project URL. Required format: https://www.{domain}/ (lowercase, trailing slash).");
        }

        var effectiveDisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName.Trim()
            : HostnameFromNormalizedUrl(normalized);

        var existing = await db.SiteProfiles
            .FirstOrDefaultAsync(p => p.SiteUrl == normalized, ct);

        if (existing is not null)
        {
            return new CreateSiteProfileResult(
                existing.Id,
                existing.SiteUrl,
                existing.DisplayName ?? effectiveDisplayName,
                Created: false);
        }

        var now = DateTime.UtcNow;
        var profile = new SiteProfile
        {
            Id = Guid.NewGuid(),
            SiteUrl = normalized,
            DisplayName = effectiveDisplayName,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.SiteProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        return new CreateSiteProfileResult(profile.Id, profile.SiteUrl, profile.DisplayName!, Created: true);
    }

    public static SiteProfileDto ToDto(SiteProfile profile) =>
        new(
            profile.Id,
            profile.SiteUrl,
            profile.GeekSeoProjectId,
            profile.DisplayName,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.BusinessProfileAt,
            profile.LastRunAt,
            profile.BusinessType,
            profile.BusinessDescription,
            profile.BusinessSummary,
            profile.PrimaryNiche,
            profile.NicheDescription,
            profile.NicheTags,
            profile.GeoAnchorNodes,
            profile.ServiceAreaDescription,
            profile.CompetitorDomains,
            profile.AuthorityPageUrls,
            profile.WritingRecommendations,
            HomepageJsonLdRecommendationBuilder.Build(profile)
                .Select(s => new RecommendedJsonLdSnippetDto(s.Id, s.Title, s.Description, s.Json, s.ScriptTag))
                .ToList());

    public async Task LinkGeekSeoProjectAsync(Guid geekSeoProjectId, string targetSiteUrl, CancellationToken ct = default)
    {
        if (geekSeoProjectId == Guid.Empty)
            throw new InvalidOperationException("Geek-SEO project id is required.");

        var normalized = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        if (string.IsNullOrEmpty(normalized))
            throw new InvalidOperationException("Project URL is required.");

        var profile = await db.SiteProfiles
            .FirstOrDefaultAsync(p => p.SiteUrl == normalized, ct);

        if (profile is null)
        {
            profile = new SiteProfile
            {
                Id = Guid.NewGuid(),
                SiteUrl = normalized,
                GeekSeoProjectId = geekSeoProjectId,
                UpdatedAt = DateTime.UtcNow,
            };
            db.SiteProfiles.Add(profile);
        }
        else
        {
            if (profile.GeekSeoProjectId is Guid linked && linked != Guid.Empty && linked != geekSeoProjectId)
            {
                throw new InvalidOperationException(
                    $"Site {normalized} is already linked to Geek-SEO project {linked}, not {geekSeoProjectId}.");
            }

            profile.GeekSeoProjectId = geekSeoProjectId;
            profile.SiteUrl = normalized;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await EnsureGeekSeoProjectRowAsync(geekSeoProjectId, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>GEEK_SEO_PROJECT_ID env is authoritative — replaces a stale sa2-only project id.</summary>
    private async Task LinkBootstrapGeekSeoProjectAsync(
        Guid geekSeoProjectId,
        string targetSiteUrl,
        CancellationToken ct = default)
    {
        var normalized = TargetSiteUrlNormalizer.Normalize(targetSiteUrl);
        if (string.IsNullOrEmpty(normalized))
            throw new InvalidOperationException("Project URL is required.");

        var profile = await db.SiteProfiles
            .FirstOrDefaultAsync(p => p.SiteUrl == normalized, ct);

        if (profile is null)
        {
            throw new InvalidOperationException(
                $"No site profile exists for {normalized}. Create a site profile before importing keywords.");
        }

        profile.GeekSeoProjectId = geekSeoProjectId;
        profile.SiteUrl = normalized;
        profile.UpdatedAt = DateTime.UtcNow;
        await EnsureGeekSeoProjectRowAsync(geekSeoProjectId, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task TouchAfterRunAsync(string normalizedUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(normalizedUrl))
            return;

        var profile = await db.SiteProfiles
            .FirstOrDefaultAsync(p => p.SiteUrl == normalizedUrl, ct);

        if (profile is null)
            return;

        var now = DateTime.UtcNow;
        profile.UpdatedAt = now;
        profile.LastRunAt = now;
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureGeekSeoProjectRowAsync(Guid geekSeoProjectId, CancellationToken ct)
    {
        if (await db.Projects.AnyAsync(p => p.Id == geekSeoProjectId, ct))
            return;

        db.Projects.Add(new Project
        {
            Id = geekSeoProjectId,
            Name = "Geek-SEO keyword import",
        });
    }

    private static string HostnameFromNormalizedUrl(string normalized)
    {
        var uri = new Uri(normalized);
        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
            host = host[4..];

        return uri.IsDefaultPort ? host : $"{host}:{uri.Port}";
    }
}
