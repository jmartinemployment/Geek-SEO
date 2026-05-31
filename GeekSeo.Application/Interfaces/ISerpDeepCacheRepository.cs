using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISerpDeepCacheRepository
{
    Task<Result<SeoSerpDeepCache?>> GetAsync(string keyword, string location, int resultCount, CancellationToken ct = default);

    Task<Result<SeoSerpDeepCache>> UpsertAsync(SeoSerpDeepCache entry, CancellationToken ct = default);
}
