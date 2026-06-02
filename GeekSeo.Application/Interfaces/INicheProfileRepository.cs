using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces;

public interface INicheProfileRepository
{
    Task<Result<NicheProfile>> CreateAsync(NicheProfile profile, CancellationToken ct = default);
    Task<Result<NicheProfile?>> GetByIdAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<NicheProfile?>> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileSummary>>> GetHistoryAsync(Guid projectId, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(Guid profileId, string status, string? step = null, int stepNumber = 0, int totalSteps = 0, string? errorMessage = null, CancellationToken ct = default);
    Task<Result> UpdateScoresAsync(Guid profileId, decimal authorityScore, int covered, int partial, int gap, CancellationToken ct = default);
    Task<Result> BulkInsertPillarsAsync(IEnumerable<NichePillar> pillars, CancellationToken ct = default);
    Task<Result> BulkInsertSubtopicsAsync(IEnumerable<NicheSubtopic> subtopics, CancellationToken ct = default);
    Task<Result> BulkInsertCompetitorsAsync(IEnumerable<NicheCompetitor> competitors, CancellationToken ct = default);
    Task<Result> BulkInsertEntitiesAsync(IEnumerable<NicheEntity> entities, CancellationToken ct = default);
    Task<Result> BulkInsertPillarPagesAsync(IEnumerable<NichePillarPage> pages, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheProfileSummary>>> ListDueForReanalysisAsync(int limit, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NicheQueuedJob>>> ListQueuedAsync(int limit, CancellationToken ct = default);
}
