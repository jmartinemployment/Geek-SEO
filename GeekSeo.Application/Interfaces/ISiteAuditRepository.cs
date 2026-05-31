using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISiteAuditRepository
{
    Task<Result<SeoSiteAudit>> CreateAsync(SeoSiteAudit audit, CancellationToken ct = default);

    Task<Result<SeoSiteAudit>> GetByIdAsync(Guid auditId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<SeoSiteAudit>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);

    Task<Result> UpdateStatusAsync(Guid auditId, UpdateSiteAuditStatusRequest request, CancellationToken ct = default);

    Task<Result> AppendPagesAsync(Guid auditId, AppendSiteAuditPagesRequest request, CancellationToken ct = default);
}
