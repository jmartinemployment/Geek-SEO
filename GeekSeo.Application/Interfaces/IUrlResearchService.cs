using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IUrlResearchService
{
    Task<Result<SeoUrlResearch>> CreateQueuedAsync(
        Guid userId, CreateUrlResearchQueuedRequest request, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> GetHeadAsync(Guid userId, Guid urlResearchId, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> GetFullAsync(Guid userId, Guid urlResearchId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<UrlResearchSummary>>> ListSummaryByProjectAsync(
        Guid userId, Guid projectId, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> PersistFullAsync(
        Guid userId, Guid urlResearchId, UrlResearchFullWrite body, CancellationToken ct = default);

    Task<Result<SeoUrlResearch>> UpdateStatusAsync(
        Guid userId, Guid urlResearchId, UrlResearchStatusPatch patch, CancellationToken ct = default);
}
