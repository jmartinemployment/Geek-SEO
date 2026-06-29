using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Infrastructure;
using SiteAnalyzer2.Services.Integrations;

namespace GeekSeoBackend.HttpClients.Repo;

public sealed class InProcessSiteAnalyzer2SiteProfileRepository(
    ContentWriterSiteBundleService siteBundles) : ISiteAnalyzer2SiteProfileRepository
{
    public async Task<Result<SiteAnalyzer2SiteProfileExport>> GetByIdAsync(
        Guid siteProfileId, CancellationToken ct = default)
    {
        var bundle = await siteBundles.GetByProfileIdAsync(siteProfileId, ct);
        return bundle is null
            ? Result<SiteAnalyzer2SiteProfileExport>.NotFound("Site profile not found")
            : Result<SiteAnalyzer2SiteProfileExport>.Success(
                SiteAnalyzer2ModelMapper.ToSiteProfileExport(bundle));
    }

    public async Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleAsync(
        Guid siteProfileId, CancellationToken ct = default)
    {
        var bundle = await siteBundles.GetByProfileIdAsync(siteProfileId, ct);
        return bundle is null
            ? Result<ContentWriterSiteBundle>.NotFound("Site profile not found")
            : Result<ContentWriterSiteBundle>.Success(SiteAnalyzer2ModelMapper.ToSiteBundle(bundle));
    }

    public async Task<Result<ContentWriterSiteBundle>> GetContentWriterBundleByGeekSeoProjectIdAsync(
        Guid geekSeoProjectId, CancellationToken ct = default)
    {
        var bundle = await siteBundles.GetByGeekSeoProjectIdAsync(geekSeoProjectId, ct);
        return bundle is null
            ? Result<ContentWriterSiteBundle>.NotFound("Site profile not found for project")
            : Result<ContentWriterSiteBundle>.Success(SiteAnalyzer2ModelMapper.ToSiteBundle(bundle));
    }
}
