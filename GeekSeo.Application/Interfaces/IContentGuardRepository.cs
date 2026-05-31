using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentGuardRepository
{
    Task<Result<SeoContentGuardPolicy?>> GetPolicyAsync(Guid projectId, CancellationToken ct = default);

    Task<Result<SeoContentGuardPolicy>> UpsertPolicyAsync(SeoContentGuardPolicy policy, CancellationToken ct = default);

    Task<Result<IReadOnlyList<SeoContentGuardRun>>> ListRunsAsync(Guid projectId, int limit, CancellationToken ct = default);

    Task<Result<SeoContentGuardRun>> CreateRunAsync(SeoContentGuardRun run, CancellationToken ct = default);

    Task<Result<SeoContentGuardRun>> UpdateRunAsync(SeoContentGuardRun run, CancellationToken ct = default);

    Task<Result<SeoContentGuardRun?>> GetRunAsync(Guid runId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ContentGuardScanCandidate>>> ListProjectsForDailyScanAsync(int limit, CancellationToken ct = default);
}

public sealed record ContentGuardScanCandidate
{
    public required Guid ProjectId { get; init; }
    public required Guid UserId { get; init; }
    public required bool AutoPatch { get; init; }
}
