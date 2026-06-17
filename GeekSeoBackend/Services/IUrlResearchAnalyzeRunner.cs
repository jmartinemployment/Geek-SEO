using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Services;

public interface IUrlResearchAnalyzeRunner
{
    Task<Result<UrlResearchFullWrite>> BuildFullWriteAsync(
        Guid userId,
        Guid projectId,
        string sourceUrl,
        CancellationToken ct = default);
}
