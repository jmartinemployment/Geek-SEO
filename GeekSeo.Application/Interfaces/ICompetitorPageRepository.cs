using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ICompetitorPageRepository
{
    Task<Result<IReadOnlyList<SeoCompetitorPage>>> GetBySerpResultAsync(Guid serpResultId, CancellationToken ct = default);

    Task<Result<SeoCompetitorPage>> UpsertAsync(
        Guid serpResultId, PageContent page, CancellationToken ct = default);
}
