using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISerpAnalysisService
{
    Task<Result<DeepSerpResult>> AnalyzeAsync(Guid userId, DeepSerpRequest request, CancellationToken ct = default);
}
