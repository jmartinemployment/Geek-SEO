using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface IAIProvider
{
    string ProviderName { get; }
    Task<Result<AIResponse>> CompleteAsync(AIRequest request, CancellationToken ct = default);
}
