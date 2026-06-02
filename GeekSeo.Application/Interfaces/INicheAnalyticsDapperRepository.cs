using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces;

public interface INicheAnalyticsDapperRepository
{
    Task<Result<NicheProfileSummary?>> GetProfileSummaryAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PillarCoverageMatrix>>> GetCoverageMatrixAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TopicalGapSummary>>> GetTopicalGapsAsync(Guid profileId, bool quickWinsOnly = false, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AuthorityProgressPoint>>> GetAuthorityProgressAsync(Guid projectId, int months = 12, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CompetitorNicheOverlap>>> GetCompetitorOverlapAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EntityCoverageReport>>> GetEntityCoverageAsync(Guid profileId, CancellationToken ct = default);
}
