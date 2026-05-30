using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISubscriptionRepository
{
    Task<Result<SeoSubscription?>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
