using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IGeoTrackingRepository
{
    Task<Result<IReadOnlyList<SeoGeoTrackingQuery>>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);

    Task<Result<SeoGeoTrackingQuery>> CreateAsync(SeoGeoTrackingQuery query, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid queryId, CancellationToken ct = default);

    Task<Result<SeoGeoTrackingQuery?>> GetQueryAsync(Guid queryId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<SeoGeoMentionSnapshot>>> ListSnapshotsAsync(
        Guid queryId,
        int days,
        CancellationToken ct = default);

    Task<Result> AddSnapshotAsync(SeoGeoMentionSnapshot snapshot, CancellationToken ct = default);

    Task<Result<IReadOnlyList<GeoProbeCandidate>>> ListEnabledQueriesAsync(int limit, CancellationToken ct = default);
}

public sealed record GeoProbeCandidate
{
    public required Guid QueryId { get; init; }
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required string QueryText { get; init; }
    public required string PlatformsJson { get; init; }
}
