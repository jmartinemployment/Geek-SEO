using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Persistence.Entities;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentFeaturedImageService
{
    Task<Result<FeaturedImageResult>> GenerateForDocumentAsync(
        Guid userId,
        Guid documentId,
        GenerateFeaturedImageRequest request,
        CancellationToken ct = default);
}
