using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentMarketingService
{
    Task<Result<ContentMarketingBundle>> GetBundleAsync(Guid userId, Guid documentId, CancellationToken ct = default);
    Task<Result<ContentMarketingBundle>> SaveBundleAsync(
        Guid userId, Guid documentId, ContentMarketingBundle bundle, CancellationToken ct = default);
    Task<Result<ContentMarketingBundle>> GenerateSummariesAsync(
        Guid userId, Guid documentId, CancellationToken ct = default);
    Task<Result<ContentMarketingBundle>> GenerateBlogSpokeAsync(
        Guid userId, Guid documentId, GenerateBlogSpokeRequest request, CancellationToken ct = default);
    Task<Result<ContentMarketingBundle>> GenerateSocialAsync(
        Guid userId, Guid documentId, CancellationToken ct = default);
    ContentMarketingValidationResult Validate(ContentMarketingBundle bundle);
}
