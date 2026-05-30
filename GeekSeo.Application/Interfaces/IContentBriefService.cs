using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentBriefService
{
    Task<Result<ContentBrief>> GenerateBriefAsync(
        Guid userId, GenerateBriefRequest request, CancellationToken ct = default);
}
