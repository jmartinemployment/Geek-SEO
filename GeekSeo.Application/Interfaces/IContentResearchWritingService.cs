using GeekSeo.Persistence.Entities;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentResearchWritingService
{
    Task<Result<SeoContentDocument>> AttachResearchAsync(
        Guid userId, Guid documentId, AttachAnalysisRunRequest request, CancellationToken ct = default);

    Task<Result<WritingTextResult>> DraftFromResearchAsync(
        Guid userId, Guid documentId, CancellationToken ct = default);
}
