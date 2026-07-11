using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces;

public interface ISiteAnalyzer2SiteProfileRepository
{
    Task<Result<SiteAnalyzer2SiteProfileExport>> GetByIdAsync(Guid siteProfileId, CancellationToken ct = default);

    Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleAsync(
        Guid siteProfileId, CancellationToken ct = default);

    Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleByGeekSeoProjectIdAsync(
        Guid geekSeoProjectId, CancellationToken ct = default);
}
