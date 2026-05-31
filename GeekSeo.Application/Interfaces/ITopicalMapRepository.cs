using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ITopicalMapRepository
{
    Task<Result<SeoTopicalMap?>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);

    Task<Result<SeoTopicalMap>> UpsertAsync(SeoTopicalMap map, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TopicalMapRefreshCandidate>>> ListDueForRefreshAsync(int limit, CancellationToken ct = default);
}

public sealed record TopicalMapRefreshCandidate
{
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid TopicalMapId { get; init; }
}
