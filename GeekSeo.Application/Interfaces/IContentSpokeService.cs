using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentSpokeService
{
    Task<Result<IReadOnlyList<ContentSpokeSummary>>> ListAsync(
        Guid userId,
        Guid pillarDocumentId,
        CancellationToken ct = default);

    Task<Result<ContentSpokeSummary>> CreateAsync(
        Guid userId,
        Guid pillarDocumentId,
        CreateContentSpokeRequest request,
        CancellationToken ct = default);

    Task<Result<ContentSpokeSummary>> GenerateAsync(
        Guid userId,
        Guid pillarDocumentId,
        Guid spokeDocumentId,
        GenerateContentSpokeRequest? request,
        CancellationToken ct = default);
}
