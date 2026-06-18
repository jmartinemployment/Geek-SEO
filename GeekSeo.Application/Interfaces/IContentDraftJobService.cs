using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentDraftJobService
{
    Task<Result<BackgroundJobStatus>> EnqueueKeywordDraftAsync(
        Guid userId, Guid documentId, KeywordContentDraftRequest request, CancellationToken ct = default);

    Task<Result<BackgroundJobStatus>> EnqueueResearchDraftAsync(
        Guid userId, Guid documentId, CancellationToken ct = default);
}
