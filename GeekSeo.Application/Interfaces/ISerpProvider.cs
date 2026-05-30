using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeo.Application.Interfaces.Seo;

public interface ISerpProvider
{
    Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default);
    string ProviderName { get; }
}
