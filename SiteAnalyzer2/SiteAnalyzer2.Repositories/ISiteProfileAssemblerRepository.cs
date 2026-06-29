using SiteAnalyzer2.Domain.Entities;

namespace SiteAnalyzer2.Repositories;

public interface ISiteProfileAssemblerRepository
{
    Task<string?> GetRunTargetSiteUrlAsync(Guid runId, CancellationToken ct = default);

    Task<Guid?> GetSiteProfileIdBySiteUrlAsync(string normalizedSiteUrl, CancellationToken ct = default);

    Task<SiteProfile?> GetSiteProfileByIdAsync(Guid siteProfileId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetSiteKeywordsAsync(string normalizedSiteUrl, CancellationToken ct = default);

    Task<SiteProfileAssemblySource> LoadAssemblySourceAsync(
        Guid siteProfileId,
        Guid runId,
        CancellationToken ct = default);

    Task PersistSiteProfileAsync(
        Guid siteProfileId,
        SiteProfileAssemblyWrite siteWrite,
        CancellationToken ct = default);

    Task PersistAsync(
        Guid siteProfileId,
        Guid runId,
        SiteProfileAssemblyWrite siteWrite,
        RunWritingFocusWrite runWrite,
        CancellationToken ct = default);
}
