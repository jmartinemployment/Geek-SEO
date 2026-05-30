using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISerpCacheRepository
{
    Task<Result<SeoSerpResult?>> GetAsync(string keyword, string location, string languageCode, CancellationToken ct = default);

    Task<Result<SeoSerpResult>> UpsertAsync(
        string keyword, string location, string languageCode,
        SerpResult serp, SerpBenchmarksPayload benchmarks,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        string keyword, string location, string languageCode,
        CancellationToken ct = default);
}
