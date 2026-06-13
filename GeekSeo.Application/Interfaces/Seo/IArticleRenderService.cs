using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IArticleRenderService
{
    Task<Result<RenderedArticleResult>> RenderAsync(
        Guid userId,
        Guid documentId,
        CancellationToken ct = default);
}
