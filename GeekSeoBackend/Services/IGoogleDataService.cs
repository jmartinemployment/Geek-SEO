using GeekSeoBackend.Models;

namespace GeekSeoBackend.Services;

public interface IGoogleDataService
{
    Task<GoogleRankingsResponse> GetRankingsAsync(
        Guid userId,
        Guid projectId,
        DateOnly? startDate,
        DateOnly? endDate,
        int? rowLimit,
        CancellationToken ct = default);

    Task<Ga4LandingPagesResponse> GetGa4LandingPagesAsync(
        Guid userId,
        Guid projectId,
        DateOnly? startDate,
        DateOnly? endDate,
        int? limit,
        CancellationToken ct = default);
}
