using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentClusterPlanService
{
    Task<Result<ContentLinkPlan>> GetAsync(Guid userId, Guid documentId, CancellationToken ct = default);

    Task<Result<ContentLinkPlan>> SaveAsync(
        Guid userId,
        Guid documentId,
        ContentLinkPlan plan,
        CancellationToken ct = default);
}
