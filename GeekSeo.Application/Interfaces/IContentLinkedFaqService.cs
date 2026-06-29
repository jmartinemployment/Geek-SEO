using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentLinkedFaqService
{
    Task<Result<GenerateLinkedFaqsResponse>> GenerateLinkedFaqsAsync(
        Guid userId,
        Guid pillarDocumentId,
        CancellationToken ct = default);
}
