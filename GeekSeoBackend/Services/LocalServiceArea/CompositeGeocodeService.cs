using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Services.LocalServiceArea;

public sealed class CompositeGeocodeService(
    GoogleGeocodeService google,
    NominatimGeocodeService nominatim,
    ILogger<CompositeGeocodeService> logger) : IGeocodeService
{
    public async Task<Result<GeoCoordinate>> GeocodeAsync(string addressQuery, CancellationToken ct = default)
    {
        var hasGoogleKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY"));
        if (hasGoogleKey)
        {
            var googleResult = await google.GeocodeAsync(addressQuery, ct);
            if (googleResult.IsSuccess)
                return googleResult;

            logger.LogWarning("Google geocode failed for {Query}: {Error}; trying Nominatim.", addressQuery, googleResult.Error);
        }

        return await nominatim.GeocodeAsync(addressQuery, ct);
    }
}
