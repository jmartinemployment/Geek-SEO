using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeoBackend.Providers.Seo.SerpApi;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>
/// Tries SerpApi first; on failure, calls DataForSEO (Phase A bridge).
/// Concrete types avoid circular DI with the database-backed <see cref="ISerpProvider"/> wrapper.
/// </summary>
public sealed class FallbackSerpProvider(SerpApiSerpProvider primary, DataForSEOSerpProvider fallback) : ISerpProvider
{
    private string _providerName = primary.ProviderName;

    public string ProviderName => _providerName;

    public async Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default)
    {
        var primaryResult = await primary.GetSerpResultsAsync(request, ct);
        if (primaryResult.IsSuccess)
        {
            _providerName = primary.ProviderName;
            return primaryResult;
        }

        var fallbackResult = await fallback.GetSerpResultsAsync(request, ct);
        if (fallbackResult.IsSuccess)
            _providerName = fallback.ProviderName;

        return fallbackResult;
    }
}
