using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IPlagiarismService
{
    PlagiarismStatus GetStatus();

    Task<Result<PlagiarismCheckResult>> CheckDocumentAsync(
        Guid userId,
        PlagiarismCheckRequest request,
        CancellationToken ct = default);

    Task<Result<PlagiarismCheckResult?>> GetLatestForDocumentAsync(
        Guid userId,
        Guid documentId,
        CancellationToken ct = default);
}
