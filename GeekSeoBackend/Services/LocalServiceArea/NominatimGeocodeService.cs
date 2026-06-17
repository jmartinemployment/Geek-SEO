using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using GeekSeo.Application.Interfaces;
using GeekSeo.Application.Models.Seo;
using GeekSeo.Application.Results;

namespace GeekSeoBackend.Services.LocalServiceArea;

/// <summary>OpenStreetMap Nominatim fallback when Google Maps API key is unavailable.</summary>
public sealed class NominatimGeocodeService(IHttpClientFactory httpClientFactory) : IGeocodeService
{
    public async Task<Result<GeoCoordinate>> GeocodeAsync(string addressQuery, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(addressQuery))
            return Result<GeoCoordinate>.Failure("Address query is required.");

        var url =
            $"https://nominatim.openstreetmap.org/search?q={HttpUtility.UrlEncode(addressQuery.Trim())}&format=json&limit=1";

        var client = httpClientFactory.CreateClient("NominatimGeocode");
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return Result<GeoCoordinate>.Failure($"Nominatim HTTP {(int)response.StatusCode}.");

        var rows = await response.Content.ReadFromJsonAsync<List<NominatimRow>>(ct);
        if (rows is null || rows.Count == 0)
            return Result<GeoCoordinate>.Failure("Nominatim returned no results.");

        if (!double.TryParse(rows[0].Lat, out var lat) || !double.TryParse(rows[0].Lon, out var lon))
            return Result<GeoCoordinate>.Failure("Nominatim returned invalid coordinates.");

        return Result<GeoCoordinate>.Success(new GeoCoordinate(lat, lon));
    }

    private sealed class NominatimRow
    {
        public string Lat { get; set; } = string.Empty;
        public string Lon { get; set; } = string.Empty;
    }
}
