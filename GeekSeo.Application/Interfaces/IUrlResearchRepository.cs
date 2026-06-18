using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IUrlResearchRepository
{
    Task<Result<SeoUrlResearch>> CreateQueuedAsync(
        Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> GetHeadAsync(Guid urlResearchId, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> GetFullAsync(Guid urlResearchId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(
        Guid projectId, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> PersistFullAsync(
        Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> UpdateStatusAsync(
        Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default);

    Task<Result<IReadOnlyList<UrlResearchQueuedJob>>> ListQueuedAsync(
        int limit, CancellationToken ct = default);

    Task<Result<int>> FailStaleRunningAsync(TimeSpan maxAge, CancellationToken ct = default);

    Task<Result<bool>> TryClaimRunningAsync(Guid urlResearchId, CancellationToken ct = default);
}
