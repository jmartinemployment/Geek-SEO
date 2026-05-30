using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IKeywordRepository
{
    Task<Result<IReadOnlyList<SeoKeyword>>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);

    Task<Result> BulkUpsertAsync(Guid projectId, IReadOnlyList<KeywordResult> keywords, string location, CancellationToken ct = default);
}
