using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IKeywordVendorSnapshotRepository
{
    Task<Result<SeoKeywordVendorSnapshot?>> GetAsync(
        string seedKeyword,
        string location,
        string languageCode,
        CancellationToken ct = default);

    Task<Result<SeoKeywordVendorSnapshot>> UpsertAsync(
        SeoKeywordVendorSnapshot entry,
        CancellationToken ct = default);
}
