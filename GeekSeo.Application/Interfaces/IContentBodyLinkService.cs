using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IContentBodyLinkService
{
    Task<Result<ApplyBodyLinksResponse>> ApplyAsync(
        Guid userId,
        Guid documentId,
        ApplyBodyLinksRequest request,
        CancellationToken ct = default);
}
