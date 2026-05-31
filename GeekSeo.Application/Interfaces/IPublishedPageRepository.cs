using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IPublishedPageRepository
{
    Task<Result<IReadOnlyList<SeoPublishedPage>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);

    Task<Result<SeoPublishedPage?>> GetByIdAsync(Guid publishedPageId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PerformanceSnapshotPoint>>> GetSparklineAsync(
        Guid publishedPageId,
        int days,
        CancellationToken ct = default);

    Task<Result> UpsertSnapshotAsync(SeoContentPerformanceSnapshot snapshot, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PublishedPageRefreshCandidate>>> ListDueForSnapshotAsync(int limit, CancellationToken ct = default);
}

public sealed record PublishedPageRefreshCandidate
{
    public required Guid PublishedPageId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required string Url { get; init; }
}
