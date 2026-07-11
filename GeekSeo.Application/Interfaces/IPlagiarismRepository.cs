using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IPlagiarismRepository
{
    Task<Result<SeoPlagiarismCheck?>> GetLatestByDocumentAsync(Guid documentId, CancellationToken ct = default);

    Task<Result<SeoPlagiarismCheck>> CreateAsync(SeoPlagiarismCheck check, CancellationToken ct = default);
}
