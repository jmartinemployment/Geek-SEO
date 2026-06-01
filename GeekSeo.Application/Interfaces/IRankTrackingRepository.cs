namespace GeekSeo.Application.Interfaces;

using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

public interface IRankTrackingRepository
{
    Task<Result<IReadOnlyList<SeoTrackedKeyword>>> GetKeywordsAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<SeoTrackedKeyword>> AddKeywordAsync(SeoTrackedKeyword entity, CancellationToken ct = default);
    Task<Result> DeleteKeywordAsync(Guid keywordId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SeoRankTracking>>> GetHistoryAsync(Guid projectId, string keyword, int days, CancellationToken ct = default);
    Task<Result> UpsertSnapshotAsync(SeoRankTracking snapshot, CancellationToken ct = default);
    Task<Result<IReadOnlyList<Guid>>> ListProjectsWithKeywordsAsync(int limit, CancellationToken ct = default);
}
