using GeekSeo.Application.Constants.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISubscriptionService
{
    Task<Result<SubscriptionTier>> GetActiveTierAsync(Guid userId, CancellationToken ct = default);
}
