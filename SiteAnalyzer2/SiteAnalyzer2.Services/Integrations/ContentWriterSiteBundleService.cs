using Microsoft.EntityFrameworkCore;
using SiteAnalyzer2.Infrastructure.Persistence;

namespace SiteAnalyzer2.Services.Integrations;

public sealed class ContentWriterSiteBundleService(AppDbContext db)
{
    public async Task<ContentWriterSiteBundleDto?> GetByProfileIdAsync(
        Guid siteProfileId,
        CancellationToken ct = default)
    {
        if (siteProfileId == Guid.Empty)
            return null;

        var profile = await db.SiteProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == siteProfileId, ct);

        return profile is null
            ? null
            : ContentWriterSiteBundleBuilder.Build(profile, DateTimeOffset.UtcNow);
    }

    public async Task<ContentWriterSiteBundleDto?> GetByGeekSeoProjectIdAsync(
        Guid geekSeoProjectId,
        CancellationToken ct = default)
    {
        if (geekSeoProjectId == Guid.Empty)
            return null;

        var profile = await db.SiteProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GeekSeoProjectId == geekSeoProjectId, ct);

        return profile is null
            ? null
            : ContentWriterSiteBundleBuilder.Build(profile, DateTimeOffset.UtcNow);
    }
}
