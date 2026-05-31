using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISiteAuditService
{
    Task<Result<SiteAuditSummaryDto>> StartAsync(Guid userId, Guid projectId, CancellationToken ct = default);

    Task<Result<SiteAuditDetailDto>> GetAsync(Guid userId, Guid auditId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<SiteAuditSummaryDto>>> ListByProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken ct = default);
}
