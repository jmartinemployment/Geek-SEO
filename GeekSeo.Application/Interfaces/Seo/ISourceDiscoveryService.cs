using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISourceDiscoveryService
{
    Task<Result<IReadOnlyList<DiscoveredSource>>> DiscoverAsync(
        Guid projectId,
        string keyword,
        string location,
        string plainTextExcerpt,
        CancellationToken ct = default);
}
