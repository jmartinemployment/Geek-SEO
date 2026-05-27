using GeekApplication.Entities.Seo;
using GeekApplication.Results;

namespace GeekSeoBackend.Services;

public interface IGoogleIntegrationRepository
{
    Task<Result<SeoGscConnection?>> GetGscConnectionAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<Result<SeoGa4Connection?>> GetGa4ConnectionAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task<Result<SeoGscConnection>> UpsertGscConnectionAsync(SeoGscConnection connection, CancellationToken ct = default);
    Task<Result<SeoGa4Connection>> UpsertGa4ConnectionAsync(SeoGa4Connection connection, CancellationToken ct = default);
    Task<Result> DeleteConnectionsAsync(Guid projectId, Guid userId, CancellationToken ct = default);
}
