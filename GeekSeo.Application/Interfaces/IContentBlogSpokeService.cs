using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentBlogSpokeService
{
    Task<Result<ContentBlogSpoke>> GetAsync(Guid userId, Guid documentId, CancellationToken ct = default);
    Task<Result<ContentBlogSpoke>> SaveAsync(
        Guid userId, Guid documentId, ContentBlogSpoke spoke, CancellationToken ct = default);
    Task<Result<ContentBlogSpoke>> GenerateAsync(
        Guid userId, Guid documentId, GenerateBlogSpokeRequest request, CancellationToken ct = default);
    ContentBlogSpokeValidationResult Validate(string pillarKeyword, ContentBlogSpoke spoke);
}
