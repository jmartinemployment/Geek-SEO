using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Providers.Seo;

/// <summary>
/// Tries primary <see cref="ISerpProvider"/> first; on failure, calls fallback (Phase A bridge).
/// </summary>
public sealed class FallbackSerpProvider(ISerpProvider primary, ISerpProvider fallback) : ISerpProvider
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
