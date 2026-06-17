using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISerpResearchPackService
{
    Task<Result<SerpResearchPack>> BuildAsync(
        Guid userId,
        UrlAnalyzerResearchRequest request,
        CancellationToken ct = default);
}
