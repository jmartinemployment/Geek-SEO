using GeekSeo.Application.Interfaces.Seo;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;
using GeekSeo.Application.Services.Seo;

namespace GeekSeoBackend.Providers.Seo.Persistence;

/// <summary>
/// Reads SERP from <c>seo_serp_results</c> when a non-expired row exists; calls vendor only on miss or expiry.
/// </summary>
public sealed class DatabaseBackedSerpProvider(ISerpProvider inner, ISerpCacheRepository cache) : ISerpProvider
{
    public string ProviderName => inner.ProviderName;

    public async Task<Result<SerpResult>> GetSerpResultsAsync(SerpRequest request, CancellationToken ct = default)
    {
        if (request.PlacesOnly)
            return await inner.GetSerpResultsAsync(request, ct);

        var keyword = request.Keyword.Trim();
        var location = request.Location.Trim();
        var languageCode = string.IsNullOrWhiteSpace(request.LanguageCode) ? "en" : request.LanguageCode.Trim();

        var cached = await cache.GetAsync(keyword, location, languageCode, ct);
        if (cached.IsSuccess && cached.Value is not null && cached.Value.ExpiresAt > DateTimeOffset.UtcNow)
        {
            var fromDb = SerpResultStore.FromDbRow(cached.Value);
            if (fromDb is not null)
                return Result<SerpResult>.Success(fromDb);
        }

        var live = await inner.GetSerpResultsAsync(request, ct);
        if (!live.IsSuccess || live.Value is null)
            return live;

        var benchmarks = SerpBenchmarkCalculator.FromSerp(live.Value);
        _ = await cache.UpsertAsync(keyword, location, languageCode, live.Value, benchmarks, ct);
        return live;
    }
}
