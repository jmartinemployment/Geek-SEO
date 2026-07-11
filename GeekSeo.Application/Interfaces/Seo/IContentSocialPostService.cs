using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentSocialPostService
{
    Task<Result<ContentSocialPostResult>> GenerateAsync(
        Guid userId,
        Guid documentId,
        GenerateSocialPostRequest request,
        CancellationToken ct = default);
}
