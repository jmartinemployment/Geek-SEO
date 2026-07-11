using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISiteResearchRepository
{
    Task<Result<SeoSiteResearch>> GetOrCreateForProjectAsync(
        Guid userId, CreateSiteResearchRequest request, CancellationToken ct = default);

    Task<Result<SeoSiteResearch>> GetWithPagesAsync(Guid siteResearchId, CancellationToken ct = default);

    Task<Result<SeoSiteResearch>> PersistStep1Async(
        Guid siteResearchId, SiteResearchStep1Write body, CancellationToken ct = default);

    Task<Result<SeoSiteResearch>> ReplacePagesAsync(
        Guid siteResearchId, IReadOnlyList<SiteResearchPageWrite> pages, CancellationToken ct = default);

    Task<Result<SeoSiteResearch>> PersistStep4Async(
        Guid siteResearchId, SiteResearchStep4Write body, CancellationToken ct = default);

    Task<Result> UpsertStepRunAsync(SiteAnalyzerStepRunUpsert upsert, CancellationToken ct = default);

    Task<Result<IReadOnlyList<SiteAnalyzerStepRunRow>>> GetStepRunsForSiteAsync(
        Guid siteResearchId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<SiteAnalyzerStepRunRow>>> GetStepRunsForPackAsync(
        Guid urlResearchId, CancellationToken ct = default);
}
